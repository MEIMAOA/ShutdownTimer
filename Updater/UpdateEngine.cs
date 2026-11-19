using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ShutdownTimer.Updater
{
    public enum UpdateStage
    {
        WaitingForMainExit,
        CleaningOldBackups,
        Extracting,
        BackingUp,
        CopyingFiles,
        Launching,
        Completed
    }

    public class UpdateProgress
    {
        public UpdateStage Stage { get; set; }
        public string Message { get; set; } = "";
        public double Percentage { get; set; } = -1;
    }

    public class UpdateEngine
    {
        public event Action<UpdateProgress>? ProgressChanged;
        public string? LastError { get; private set; }

        public bool Run(string appDir, string zipPath, string exeName)
        {
            LastError = null;
            string? extractDir = null;
            string? backupDir = null;

            try
            {
                appDir = appDir.TrimEnd('\\', '/');
                string processName = Path.GetFileNameWithoutExtension(exeName);

                Report(UpdateStage.WaitingForMainExit, "正在等待主程序退出...", -1);
                WaitForProcessExit(processName, 60000);
                Thread.Sleep(1500);

                Report(UpdateStage.CleaningOldBackups, "正在清理旧的更新备份...", -1);
                CleanupOldBackups(appDir);

                Report(UpdateStage.Extracting, "正在解压更新包...", 0);
                extractDir = Path.Combine(Path.GetTempPath(),
                    "ShutdownTimerUpdate_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(extractDir);

                try { ZipFile.ExtractToDirectory(zipPath, extractDir); }
                catch (Exception ex) { LastError = $"解压失败: {ex.Message}"; return false; }

                Report(UpdateStage.BackingUp, "正在备份旧文件...", -1);
                backupDir = Path.Combine(appDir, "_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupDir);

                string[] files = Directory.GetFiles(extractDir, "*", SearchOption.AllDirectories);
                int total = files.Length;
                int processed = 0;

                foreach (string srcFile in files)
                {
                    string relPath = srcFile.Substring(extractDir.Length).TrimStart('\\', '/');
                    string dstFile = Path.Combine(appDir, relPath);
                    if (File.Exists(dstFile))
                    {
                        string backupFile = Path.Combine(backupDir, relPath);
                        string? backupSubDir = Path.GetDirectoryName(backupFile);
                        if (!string.IsNullOrEmpty(backupSubDir) && !Directory.Exists(backupSubDir))
                            Directory.CreateDirectory(backupSubDir);
                        try { File.Copy(dstFile, backupFile, overwrite: true); } catch { }
                    }
                }

                Report(UpdateStage.CopyingFiles, "正在替换文件...", 0);
                foreach (string srcFile in files)
                {
                    string relPath = srcFile.Substring(extractDir.Length).TrimStart('\\', '/');
                    string dstFile = Path.Combine(appDir, relPath);

                    string? dstSubDir = Path.GetDirectoryName(dstFile);
                    if (!string.IsNullOrEmpty(dstSubDir) && !Directory.Exists(dstSubDir))
                        Directory.CreateDirectory(dstSubDir);

                    if (File.Exists(dstFile))
                    {
                        try { File.SetAttributes(dstFile, FileAttributes.Normal); } catch { }
                    }

                    try { File.Copy(srcFile, dstFile, overwrite: true); }
                    catch (Exception ex)
                    {
                        LastError = $"替换文件失败 ({relPath}): {ex.Message}";
                        TryRollback(backupDir, appDir);
                        return false;
                    }

                    processed++;
                    double pct = total > 0 ? (processed * 100.0 / total) : 100;
                    Report(UpdateStage.CopyingFiles,
                        $"正在替换文件 ({processed}/{total})...", pct);
                }

                Report(UpdateStage.Launching, "正在启动新版本...", 100);
                string newExePath = Path.Combine(appDir, exeName);
                if (!File.Exists(newExePath))
                {
                    LastError = $"新版本主程序未找到: {newExePath}";
                    TryRollback(backupDir, appDir);
                    return false;
                }

                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = newExePath,
                        WorkingDirectory = appDir,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    LastError = $"启动新版本失败: {ex.Message}";
                    return false;
                }

                TryCleanup(extractDir, zipPath);

                Report(UpdateStage.Completed, "更新完成", 100);
                Thread.Sleep(800);
                return true;
            }
            catch (Exception ex)
            {
                LastError = $"更新器异常: {ex.Message}";
                return false;
            }
        }

        private void Report(UpdateStage stage, string message, double percentage)
        {
            ProgressChanged?.Invoke(new UpdateProgress
            {
                Stage = stage,
                Message = message,
                Percentage = percentage
            });
        }

        private static void WaitForProcessExit(string processName, int maxWaitMs)
        {
            int waited = 0;
            const int intervalMs = 300;

            while (waited < maxWaitMs)
            {
                var procs = Process.GetProcessesByName(processName);
                if (procs.Length == 0)
                {
                    foreach (var p in procs) p.Dispose();
                    return;
                }
                foreach (var p in procs) p.Dispose();
                Thread.Sleep(intervalMs);
                waited += intervalMs;
            }

            var remaining = Process.GetProcessesByName(processName);
            foreach (var p in remaining)
            {
                try { p.Kill(); } catch { }
                p.Dispose();
            }
            Thread.Sleep(1000);
        }

        private static void CleanupOldBackups(string appDir)
        {
            try
            {
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
                    try
                    {
                        File.SetAttributes(file, FileAttributes.Normal);
                        File.Delete(file);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void TryRollback(string backupDir, string appDir)
        {
            try
            {
                if (!Directory.Exists(backupDir)) return;
                foreach (string filePath in Directory.GetFiles(backupDir, "*", SearchOption.AllDirectories))
                {
                    string rel = filePath.Substring(backupDir.Length).TrimStart('\\', '/');
                    string dst = Path.Combine(appDir, rel);
                    string? dstDir = Path.GetDirectoryName(dst);
                    if (!string.IsNullOrEmpty(dstDir) && !Directory.Exists(dstDir))
                        Directory.CreateDirectory(dstDir);
                    try { File.Copy(filePath, dst, overwrite: true); } catch { }
                }
            }
            catch { }
        }

        private static void TryCleanup(string extractDir, string zipPath)
        {
            try { if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true); } catch { }
            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
        }
    }
}