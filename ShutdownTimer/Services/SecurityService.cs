using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ShutdownTimer.Services
{
    public class DefenderStatus
    {
        public bool RealTimeEnabled { get; set; }
        public string AntivirusEnabled { get; set; } = "未知";
        public string SignatureLastUpdated { get; set; } = "未知";
        public string Message { get; set; } = "";
    }

    public static class SecurityService
    {
        public static async Task<DefenderStatus> GetDefenderStatusAsync()
        {
            var status = new DefenderStatus();
            try
            {
                string script =
                    "try {" +
                    " $s = Get-MpComputerStatus;" +
                    " Write-Output ('RT=' + $s.RealTimeProtectionEnabled);" +
                    " Write-Output ('AV=' + $s.AntivirusEnabled);" +
                    " Write-Output ('SU=' + $s.AntivirusSignatureLastUpdated);" +
                    "} catch { Write-Output 'ERROR=' + $_.Exception.Message }";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return status;

                string output = await proc.StandardOutput.ReadToEndAsync();
                await Task.Run(() => proc.WaitForExit(5000));

                foreach (var line in output.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.StartsWith("RT=")) status.RealTimeEnabled = t.EndsWith("True");
                    else if (t.StartsWith("AV=")) status.AntivirusEnabled = t.Substring(3);
                    else if (t.StartsWith("SU=")) status.SignatureLastUpdated = t.Substring(3);
                    else if (t.StartsWith("ERROR=")) status.Message = t.Substring(6);
                }
            }
            catch (Exception ex)
            {
                status.Message = ex.Message;
            }
            return status;
        }
    }
}