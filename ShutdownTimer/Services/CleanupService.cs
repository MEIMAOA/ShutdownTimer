using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ShutdownTimer.Services
{
    public class LargeFile
    {
        public string Path { get; set; } = "";
        public long SizeBytes { get; set; }
        public string SizeDisplay => SystemInfoService.FormatBytes(SizeBytes);
    }

    public static class CleanupService
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        // ---- 计算目录大小 ----
        public static long GetDirectorySize(string path)
        {
            long total = 0;
            try
            {
                if (!Directory.Exists(path)) return 0;
                var di = new DirectoryInfo(path);
                foreach (var f in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try { total += f.Length; } catch { }
                }
            }
            catch { }
            return total;
        }

        // ---- 清理 Temp ----
        public static (int deleted, long freedBytes) CleanTempFolders()
        {
            int count = 0;
            long freed = 0;

            var folders = new[]
            {
                Path.GetTempPath(),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
            };

            foreach (var folder in folders)
            {
                if (!Directory.Exists(folder)) continue;

                try
                {
                    foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            var fi = new FileInfo(file);
                            long size = fi.Length;
                            fi.Delete();
                            count++;
                            freed += size;
                        }
                        catch { }
                    }
                }
                catch { }
            }
            return (count, freed);
        }

        // ---- 清空回收站 ----
        public static bool EmptyRecycleBin()
        {
            try
            {
                int hr = SHEmptyRecycleBin(IntPtr.Zero, null,
                    SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                return hr == 0;
            }
            catch { return false; }
        }

        // ---- 扫描大文件 ----
        public static List<LargeFile> ScanLargeFiles(string rootPath, long minBytes, int maxResults, Action<string>? progress = null)
        {
            var result = new List<LargeFile>();
            if (!Directory.Exists(rootPath)) return result;

            try
            {
                var stack = new Stack<string>();
                stack.Push(rootPath);

                while (stack.Count > 0 && result.Count < maxResults * 4)
                {
                    var dir = stack.Pop();
                    progress?.Invoke(dir);

                    try
                    {
                        foreach (var sub in Directory.GetDirectories(dir))
                        {
                            // 跳过系统保护目录
                            if (sub.Contains("$Recycle.Bin")) continue;
                            if (sub.Contains("\\Windows\\")) continue;
                            stack.Push(sub);
                        }
                    }
                    catch { }

                    try
                    {
                        foreach (var f in Directory.GetFiles(dir))
                        {
                            try
                            {
                                var fi = new FileInfo(f);
                                if (fi.Length >= minBytes)
                                {
                                    result.Add(new LargeFile
                                    {
                                        Path = f,
                                        SizeBytes = fi.Length
                                    });
                                }
                            }
                            catch { }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            result.Sort((a, b) => b.SizeBytes.CompareTo(a.SizeBytes));
            if (result.Count > maxResults) result = result.GetRange(0, maxResults);
            return result;
        }
    }
}