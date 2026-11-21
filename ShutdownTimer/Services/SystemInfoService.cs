using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ShutdownTimer.Services
{
    public class DiskInfo
    {
        public string Name { get; set; } = "";
        public string Label { get; set; } = "";
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }

        public double UsedPercent =>
            TotalBytes > 0 ? (TotalBytes - FreeBytes) * 100.0 / TotalBytes : 0;

        // ★ 加类名前缀，指向 SystemInfoService.FormatBytes
        public string TotalDisplay => SystemInfoService.FormatBytes(TotalBytes);
        public string FreeDisplay => SystemInfoService.FormatBytes(FreeBytes);
        public string UsedDisplay => SystemInfoService.FormatBytes(TotalBytes - FreeBytes);
        public string UsedPercentText => $"{UsedPercent:0}%";
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public long MemoryMB { get; set; }
        public string MemoryDisplay => $"{MemoryMB} MB";
    }

    public static class SystemInfoService
    {
        [StructLayout(LayoutKind.Sequential)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int u = 0;
            while (size >= 1024 && u < units.Length - 1) { size /= 1024; u++; }
            return $"{size:0.##} {units[u]}";
        }

        // ---- OS ----
        public static string GetOsName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                var productName = key?.GetValue("ProductName")?.ToString() ?? "Windows";
                var displayVersion = key?.GetValue("DisplayVersion")?.ToString() ?? "";
                var build = key?.GetValue("CurrentBuildNumber")?.ToString() ?? "";
                return $"{productName} {displayVersion} (Build {build})";
            }
            catch { return "Windows"; }
        }

        // ---- CPU ----
        public static string GetCpuName()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "未知";
            }
            catch { return "未知"; }
        }

        public static int GetCpuCores() => Environment.ProcessorCount;

        // ---- 内存 ----
        public static (long totalBytes, long availBytes, double usedPercent) GetMemory()
        {
            try
            {
                var m = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(m))
                    return ((long)m.ullTotalPhys, (long)m.ullAvailPhys, m.dwMemoryLoad);
            }
            catch { }
            return (0, 0, 0);
        }

        // ---- 磁盘 ----
        public static List<DiskInfo> GetDisks()
        {
            var list = new List<DiskInfo>();
            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady) continue;
                    if (drive.DriveType != DriveType.Fixed &&
                        drive.DriveType != DriveType.Removable) continue;

                    list.Add(new DiskInfo
                    {
                        Name = drive.Name,
                        Label = drive.VolumeLabel ?? "",
                        TotalBytes = drive.TotalSize,
                        FreeBytes = drive.AvailableFreeSpace
                    });
                }
            }
            catch { }
            return list;
        }

        // ---- 开机时长 ----
        public static TimeSpan GetUptime()
        {
            try { return TimeSpan.FromMilliseconds(Environment.TickCount64); }
            catch { return TimeSpan.Zero; }
        }

        public static string FormatUptime(TimeSpan t)
        {
            if (t.TotalDays >= 1) return $"{(int)t.TotalDays} {Loc.T("天")} {t.Hours} {Loc.T("小时")} {t.Minutes} {Loc.T("分")}";
            if (t.TotalHours >= 1) return $"{t.Hours} {Loc.T("小时")} {t.Minutes} {Loc.T("分")}";
            return $"{t.Minutes} {Loc.T("分")} {t.Seconds} {Loc.T("秒")}";
        }

        // ---- 进程 ----
        public static List<ProcessInfo> GetProcesses()
        {
            var list = new List<ProcessInfo>();
            try
            {
                foreach (var p in System.Diagnostics.Process.GetProcesses())
                {
                    try
                    {
                        list.Add(new ProcessInfo
                        {
                            Id = p.Id,
                            Name = p.ProcessName,
                            MemoryMB = p.WorkingSet64 / (1024 * 1024)
                        });
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
                list.Sort((a, b) => b.MemoryMB.CompareTo(a.MemoryMB));
            }
            catch { }
            return list;
        }
    }
}