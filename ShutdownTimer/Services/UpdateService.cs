using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ShutdownTimer.Services
{
    // ==================== GitHub API DTO ====================

    internal class GitHubReleaseInfo
    {
        public string? tag_name { get; set; }
        public string? name { get; set; }
        public string? body { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public string? published_at { get; set; }
        public GitHubAsset[]? assets { get; set; }
    }

    internal class GitHubAsset
    {
        public string? name { get; set; }
        public string? browser_download_url { get; set; }
        public long size { get; set; }
        public string? content_type { get; set; }
    }

    public class UpdateCheckResult
    {
        public bool HasUpdate { get; set; }
        public string? LatestVersion { get; set; }
        public string? CurrentVersion { get; set; }
        public string? ReleaseNotes { get; set; }
        public string? DownloadUrl { get; set; }
        public long FileSize { get; set; }
        public string? Sha256 { get; set; }
        public string? ReleasePageUrl { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>是否为渠道切换（切换时允许降级）。</summary>
        public bool IsChannelSwitch { get; set; }
    }

    public static class UpdateService
    {
        // ============================================================
        // ★ 仓库信息（按更新渠道选择）
        // ============================================================
        private const string GitHubOwner = "MEIMAOA";

        /// <summary>正式版仓库</summary>
        private const string RepoOfficial = "ShutdownTimer";

        /// <summary>教育分支版仓库</summary>
        private const string RepoEducation = "ShutdownTimer-Edu";

        public const string ChannelOfficial = "Official";
        public const string ChannelEducation = "Education";

        private const string AssetNameKeyword = "ShutdownTimer";
        private const string MainExeName = "ShutdownTimer.exe";
        private const string UpdaterExeName = "ShutdownTimer.Updater.exe";

        private static readonly HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                UseProxy = true
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ShutdownTimer-Updater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip");
            return client;
        }

        /// <summary>根据当前配置的更新渠道返回对应仓库名。</summary>
        public static string GetCurrentRepo()
        {
            string channel = AppConfig.Load().UpdateChannel ?? BuildInfo.DefaultChannel;
            return string.Equals(channel, ChannelEducation, StringComparison.OrdinalIgnoreCase)
                ? RepoEducation
                : RepoOfficial;
        }

        /// <summary>当前渠道的显示名。</summary>
        public static string GetChannelDisplayName()
            => string.Equals(AppConfig.Load().UpdateChannel, ChannelEducation, StringComparison.OrdinalIgnoreCase)
                ? "教育分支版"
                : "正式版";

        public static string GetReleasePageUrl()
            => $"https://github.com/{GitHubOwner}/{GetCurrentRepo()}/releases";

        // ==================== 版本 ====================

        public static Version GetCurrentVersion()
        {
            try
            {
                var fileVersion = FileVersionInfo
                    .GetVersionInfo(Assembly.GetExecutingAssembly().Location)
                    .FileVersion;
                if (!string.IsNullOrWhiteSpace(fileVersion) && Version.TryParse(fileVersion, out var v1))
                    return Normalize(v1);
            }
            catch { }

            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null) return Normalize(v);
            }
            catch { }

            return new Version(0, 0, 0, 0);
        }

        private static Version Normalize(Version v)
            => new Version(
                Math.Max(v.Major, 0),
                Math.Max(v.Minor, 0),
                Math.Max(v.Build, 0),
                Math.Max(v.Revision, 0));

        // ==================== 检查更新 ====================

        public static async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = GetCurrentVersion().ToString(),
                ReleasePageUrl = GetReleasePageUrl()
            };

            try
            {
                string apiUrl = $"https://api.github.com/repos/{GitHubOwner}/{GetCurrentRepo()}/releases/latest";
                using var response = await _http.GetAsync(apiUrl).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        // 尚无 Release，视为无更新
                        result.HasUpdate = false;
                        return result;
                    }
                    result.ErrorMessage = $"GitHub API 返回 {(int)response.StatusCode}";
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var release = JsonSerializer.Deserialize<GitHubReleaseInfo>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (release == null || string.IsNullOrWhiteSpace(release.tag_name))
                {
                    result.ErrorMessage = "未获取到有效的 Release 信息";
                    return result;
                }

                if (release.draft || release.prerelease)
                {
                    result.ErrorMessage = "最新 Release 为草稿或预发布，已跳过";
                    return result;
                }

                string tagClean = release.tag_name.Trim().TrimStart('v', 'V');
                var latestVersion = ParseVersion(tagClean);
                if (latestVersion == null)
                {
                    result.ErrorMessage = $"无法解析版本号: {release.tag_name}";
                    return result;
                }

                result.LatestVersion = latestVersion.ToString();
                result.ReleaseNotes = release.body ?? "";

                // 渠道切换：当前构建所属渠道 ≠ 用户选择的渠道 → 允许双向切换（含降级）
                string currentBuildChannel = BuildInfo.IsEducation ? ChannelEducation : ChannelOfficial;
                string selectedChannel = AppConfig.Load().UpdateChannel ?? BuildInfo.DefaultChannel;
                bool isChannelSwitch = !string.Equals(
                    currentBuildChannel, selectedChannel, StringComparison.OrdinalIgnoreCase);

                if (!isChannelSwitch && latestVersion <= GetCurrentVersion())
                {
                    result.HasUpdate = false;
                    return result;
                }

                result.IsChannelSwitch = isChannelSwitch;

                if (release.assets != null)
                {
                    foreach (var asset in release.assets)
                    {
                        if (string.IsNullOrEmpty(asset.name)) continue;
                        if (!asset.name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;
                        if (asset.name.IndexOf(AssetNameKeyword, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        result.DownloadUrl = asset.browser_download_url;
                        result.FileSize = asset.size;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(result.DownloadUrl))
                {
                    result.ErrorMessage = "Release 中未找到匹配的 zip 资源";
                    return result;
                }

                if (!string.IsNullOrEmpty(result.ReleaseNotes))
                {
                    var m = Regex.Match(result.ReleaseNotes,
                        @"<!--\s*SHA256\s*:\s*([a-fA-F0-9]{64})\s*-->",
                        RegexOptions.IgnoreCase);
                    if (m.Success) result.Sha256 = m.Groups[1].Value.ToLowerInvariant();
                }

                result.HasUpdate = true;
                result.ErrorMessage = null;
                return result;
            }
            catch (TaskCanceledException)
            {
                result.ErrorMessage = "检查更新超时，请检查网络";
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"检查更新失败: {ex.Message}";
                return result;
            }
        }

        // ==================== 下载 ====================

        public static async Task<string> DownloadUpdateAsync(
            string downloadUrl,
            IProgress<double>? progress,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
                throw new ArgumentException("下载地址为空", nameof(downloadUrl));

            string tempDir = Path.Combine(Path.GetTempPath(), "ShutdownTimerUpdate");
            Directory.CreateDirectory(tempDir);

            try
            {
                foreach (var old in Directory.GetFiles(tempDir, "update_*.zip"))
                {
                    try { File.Delete(old); } catch { }
                }
            }
            catch { }

            string tempPath = Path.Combine(tempDir,
                $"update_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}.zip");

            using var response = await _http.GetAsync(downloadUrl,
                HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long totalBytes = response.Content.Headers.ContentLength ?? -1L;

            using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (var fileStream = new FileStream(tempPath, FileMode.Create,
                FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                byte[] buffer = new byte[81920];
                long totalRead = 0L;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, ct)
                    .ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                    totalRead += bytesRead;

                    if (totalBytes > 0 && progress != null)
                    {
                        double percent = (double)totalRead / totalBytes * 100.0;
                        progress.Report(Math.Min(percent, 100.0));
                    }
                }

                await fileStream.FlushAsync(ct).ConfigureAwait(false);
            }

            progress?.Report(100.0);
            return tempPath;
        }

        // ==================== SHA256 校验 ====================

        public static bool VerifySha256(string filePath, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256)) return true;
            if (!File.Exists(filePath)) return false;

            try
            {
                using var stream = new FileStream(filePath, FileMode.Open,
                    FileAccess.Read, FileShare.Read, 81920, useAsync: false);
                using var sha = SHA256.Create();
                byte[] hashBytes = sha.ComputeHash(stream);
                string computed = BitConverter.ToString(hashBytes)
                    .Replace("-", string.Empty).ToLowerInvariant();
                return string.Equals(computed, expectedSha256.Trim().ToLowerInvariant(),
                    StringComparison.Ordinal);
            }
            catch { return false; }
        }

        // ==================== 启动 Updater ====================

        public static void LaunchUpdater(string zipPath)
        {
            string logPath = Path.Combine(Path.GetTempPath(), "ShutdownTimer_Update.log");
            void Log(string msg)
            {
                try
                {
                    File.AppendAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {msg}{Environment.NewLine}");
                }
                catch { }
            }

            Log("=== LaunchUpdater 进入 ===");
            Log($"zipPath = {zipPath}");

            if (string.IsNullOrWhiteSpace(zipPath))
                throw new ArgumentException("zip 路径为空", nameof(zipPath));
            if (!File.Exists(zipPath))
                throw new FileNotFoundException("更新包不存在", zipPath);

            string appDir = AppContext.BaseDirectory.TrimEnd('\\', '/');
            Log($"appDir = {appDir}");

            string currentExeName = MainExeName;
            try
            {
                var mainModule = Process.GetCurrentProcess().MainModule;
                if (mainModule != null && !string.IsNullOrEmpty(mainModule.FileName))
                    currentExeName = Path.GetFileName(mainModule.FileName);
            }
            catch { }
            Log($"currentExeName = {currentExeName}");

            // ★ Updater 是子目录
            string updaterSrcDir = Path.Combine(appDir, "Updater");
            string updaterExeInSrc = Path.Combine(updaterSrcDir, "ShutdownTimer.Updater.exe");

            Log($"updaterSrcDir = {updaterSrcDir}");
            Log($"updaterExeInSrc 存在 = {File.Exists(updaterExeInSrc)}");

            if (!File.Exists(updaterExeInSrc))
            {
                throw new FileNotFoundException(
                    $"Updater 目录不存在或不完整。\n" +
                    $"查找路径：{updaterExeInSrc}\n" +
                    "请确认发布包中包含了 Updater 子目录。", updaterExeInSrc);
            }

            // 创建临时目录
            string tempDir = Path.Combine(Path.GetTempPath(),
                "ShutdownTimerUpdater_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tempDir);
            Log($"tempDir = {tempDir}");

            // ★ 复制整个 Updater 目录到 %TEMP%
            try
            {
                foreach (var file in Directory.GetFiles(updaterSrcDir, "*", SearchOption.AllDirectories))
                {
                    string rel = Path.GetRelativePath(updaterSrcDir, file);
                    string dst = Path.Combine(tempDir, rel);
                    string? dstDir = Path.GetDirectoryName(dst);
                    if (!string.IsNullOrEmpty(dstDir))
                        Directory.CreateDirectory(dstDir);
                    File.Copy(file, dst, overwrite: true);
                }
                Log($"Updater 目录已复制到: {tempDir}");
            }
            catch (Exception ex)
            {
                Log($"!! 复制 Updater 目录失败: {ex}");
                throw new InvalidOperationException($"复制 Updater 目录失败: {ex.Message}", ex);
            }

            string updaterTempPath = Path.Combine(tempDir, "ShutdownTimer.Updater.exe");

            // 读主题
            string themeMode = "System";
            string accentColor = "#0078d4";
            try
            {
                var cfg = AppConfig.Load();
                if (cfg != null)
                {
                    if (!string.IsNullOrWhiteSpace(cfg.AppTheme)) themeMode = cfg.AppTheme;
                }
            }
            catch { }

            string arguments =
                $"--app-dir \"{appDir}\" " +
                $"--zip \"{zipPath}\" " +
                $"--exe \"{currentExeName}\" " +
                $"--temp-dir \"{tempDir}\" " +
                $"--theme \"{themeMode}\" " +
                $"--accent \"{accentColor}\"";

            Log($"启动命令: {updaterTempPath} {arguments}");

            var psi = new ProcessStartInfo
            {
                FileName = updaterTempPath,
                Arguments = arguments,
                UseShellExecute = true,
                WorkingDirectory = tempDir,
                CreateNoWindow = false
            };

            try
            {
                var proc = Process.Start(psi);
                Log($"Process.Start 返回: PID={proc?.Id}");
                if (proc == null)
                {
                    Log("!! Process.Start 返回 null");
                    throw new InvalidOperationException("Process.Start 返回 null");
                }
            }
            catch (Exception ex)
            {
                Log($"!! Process.Start 异常: {ex}");
                throw new InvalidOperationException($"启动更新程序失败: {ex.Message}", ex);
            }

            Log("LaunchUpdater 完成");
        }

        // ==================== 清理 ====================

        public static void CleanupTempFiles()
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ShutdownTimerUpdate");
                if (!Directory.Exists(tempDir)) return;

                var cutoff = DateTime.Now.AddHours(-24);
                foreach (var file in Directory.GetFiles(tempDir))
                {
                    try { if (File.GetLastWriteTime(file) < cutoff) File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        public static void CleanupUpdaterTempDirs()
        {
            try
            {
                string tempRoot = Path.GetTempPath();
                if (!Directory.Exists(tempRoot)) return;

                foreach (var dir in Directory.GetDirectories(tempRoot, "ShutdownTimerUpdater_*"))
                {
                    try
                    {
                        if (Directory.GetCreationTime(dir) < DateTime.Now.AddHours(-24))
                            Directory.Delete(dir, true);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public static void CleanupOldBackups()
        {
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');

                foreach (var dir in Directory.GetDirectories(appDir, "_backup_*"))
                {
                    try
                    {
                        foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(f, FileAttributes.Normal); } catch { }
                        }
                        Directory.Delete(dir, true);
                    }
                    catch { }
                }

                foreach (var file in Directory.GetFiles(appDir, "*.old_*"))
                {
                    try { File.SetAttributes(file, FileAttributes.Normal); File.Delete(file); }
                    catch { }
                }
            }
            catch { }
        }

        // ==================== 版本解析 ====================

        private static Version? ParseVersion(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();

            if (!Version.TryParse(s, out Version? v))
            {
                var m = Regex.Match(s, @"^(\d+(?:\.\d+){0,3})");
                if (!m.Success) return null;
                if (!Version.TryParse(m.Groups[1].Value, out v)) return null;
            }

            return Normalize(v);
        }
    }
}