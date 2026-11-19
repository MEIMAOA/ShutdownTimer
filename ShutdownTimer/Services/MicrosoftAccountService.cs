using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShutdownTimer.Services
{
    /// <summary>当前 Windows 登录账户信息。</summary>
    public class AccountInfo
    {
        public string? PrincipalSource { get; set; }
        public string? LocalName { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarBase64 { get; set; }

        public bool IsMicrosoftAccount =>
            string.Equals(PrincipalSource, "MicrosoftAccount", StringComparison.OrdinalIgnoreCase);

        /// <summary>优先使用微软账户显示名，否则回退本地用户名。</summary>
        public string EffectiveName =>
            string.IsNullOrWhiteSpace(DisplayName) ? (LocalName ?? "") : DisplayName!;
    }

    public static class MicrosoftAccountService
    {
        /// <summary>
        /// 判断当前登录用户是否为 Microsoft 账户。
        /// 用 PowerShell 查 LocalUser 的 PrincipalSource。
        /// </summary>
        public static async Task<bool> IsMicrosoftAccountAsync()
        {
            try
            {
                string script =
                    "$u = Get-LocalUser -Name $env:USERNAME -ErrorAction SilentlyContinue; " +
                    "if ($u) { Write-Output $u.PrincipalSource } else { Write-Output 'Unknown' }";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return false;

                string output = await proc.StandardOutput.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit(5000));

                return output.Trim().Equals("MicrosoftAccount", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>
        /// 一次性检测当前账户：是否为微软账户、显示名、以及账户头像。
        /// 结果通过 base64(JSON) 返回，避免编码问题。
        /// </summary>
        public static async Task<AccountInfo?> GetAccountInfoAsync()
        {
            try
            {
                string script = @"$ErrorActionPreference='SilentlyContinue';" +
                    @"$sid=[System.Security.Principal.WindowsIdentity]::GetCurrent().User.Value;" +
                    @"$u=Get-LocalUser -Name $env:USERNAME;" +
                    @"$src=if($u){[string]$u.PrincipalSource}else{'Unknown'};" +
                    @"$displayName='';" +
                    @"if($src -eq 'MicrosoftAccount'){" +
                    @"$k='HKCU:\Software\Microsoft\IdentityCRL\UserExtendedProperties\'+$sid;" +
                    @"$p=Get-ItemProperty -Path $k -ErrorAction SilentlyContinue;" +
                    @"if($p){$displayName=[string]$p.DisplayName;" +
                    @"if(-not $displayName){$displayName=([string]$p.FirstName+' '+[string]$p.LastName).Trim()}}};" +
                    @"$avatarB64='';" +
                    @"if($src -eq 'MicrosoftAccount'){" +
                    @"$k2='HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AccountPicture\Users\'+$sid;" +
                    @"$ap=Get-ItemProperty -Path $k2 -ErrorAction SilentlyContinue;" +
                    @"if($ap){foreach($n in @('Image448','Image240','Image200','Image96')){" +
                    @"$b=$ap.$n;if($b){$avatarB64=[Convert]::ToBase64String($b);break}}}};" +
                    @"$o=[PSCustomObject]@{PrincipalSource=$src;LocalName=$env:USERNAME;DisplayName=$displayName;AvatarBase64=$avatarB64};" +
                    @"$json=$o|ConvertTo-Json -Compress;" +
                    @"[Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($json))";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                string output = await proc.StandardOutput.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit(8000));

                output = output.Trim();
                if (string.IsNullOrWhiteSpace(output)) return null;

                byte[] jsonBytes = Convert.FromBase64String(output);
                string json = Encoding.UTF8.GetString(jsonBytes);

                return JsonSerializer.Deserialize<AccountInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }
    }
}
