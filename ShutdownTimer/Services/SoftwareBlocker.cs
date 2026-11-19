using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 教育分支版专属：禁止打开特定软件。
    /// - 通过 WMI Win32_ProcessStartTrace 监听进程启动，进程一启动即被结束（即时拦截）；
    /// - 同时保留周期扫描兜底（含完整路径匹配）。
    /// 仅教育分支版启用（正式版中直接跳过）。
    /// </summary>
    public static class SoftwareBlocker
    {
        private static readonly object _lock = new();
        private static ManagementEventWatcher? _watcher;
        private static HashSet<string> _blockedNames = new(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _blockedPaths = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>启动拦截（应用启动时调用一次）。</summary>
        public static void Start()
        {
            if (!BuildInfo.IsEducation) return;

            ReloadBlockList();
            StartWatcher();
        }

        /// <summary>周期调用（兜底扫描，建议每 10 秒一次）。</summary>
        public static void Tick()
        {
            if (!BuildInfo.IsEducation) return;

            ReloadBlockList();
            KillBlockedProcesses();
        }

        // ==================== 黑名单 ====================

        private static void ReloadBlockList()
        {
            try
            {
                var c = AppConfig.Load();
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (c.BlockedApps != null)
                {
                    foreach (var raw in c.BlockedApps)
                    {
                        string s = (raw ?? "").Trim();
                        if (s.Length == 0) continue;

                        // 含路径分隔符 → 按完整路径匹配；否则按进程名匹配
                        if (s.IndexOf('\\') >= 0 || s.IndexOf('/') >= 0)
                        {
                            paths.Add(Path.GetFullPath(s));
                        }
                        else
                        {
                            names.Add(StripExe(s));
                        }
                    }
                }

                lock (_lock)
                {
                    _blockedNames = names;
                    _blockedPaths = paths;
                }
            }
            catch { }
        }

        private static string StripExe(string s)
            => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? s.Substring(0, s.Length - 4)
                : s;

        // ==================== WMI 进程启动监听 ====================

        private static void StartWatcher()
        {
            try
            {
                _watcher?.Dispose();
                var query = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                _watcher = new ManagementEventWatcher(query);
                _watcher.EventArrived += OnProcessStarted;
                _watcher.Start();
            }
            catch { }
        }

        private static void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string? pidStr = e.NewEvent.Properties["ProcessID"]?.Value?.ToString();
                string? processName = e.NewEvent.Properties["ProcessName"]?.Value?.ToString();

                int pid = int.TryParse(pidStr, out int parsed) ? parsed : -1;

                if (string.IsNullOrEmpty(processName)) return;

                if (IsBlocked(processName))
                {
                    KillProcess(pid, processName);
                }
            }
            catch { }
        }

        private static bool IsBlocked(string processName)
        {
            HashSet<string> names, paths;
            lock (_lock)
            {
                names = _blockedNames;
                paths = _blockedPaths;
            }

            // ProcessName 可能是完整路径，也可能是纯名称
            if (processName.IndexOf('\\') >= 0)
            {
                return paths.Contains(Path.GetFullPath(processName))
                    || names.Contains(Path.GetFileNameWithoutExtension(processName));
            }

            return names.Contains(StripExe(processName));
        }

        private static void KillProcess(int pid, string processName)
        {
            try
            {
                if (pid > 0)
                {
                    using var p = Process.GetProcessById(pid);
                    try { if (!p.HasExited) p.Kill(); } catch { }
                }
            }
            catch { }

            // 兜底：按名字再结束一遍
            try
            {
                string pure = Path.GetFileNameWithoutExtension(processName);
                foreach (var p in Process.GetProcessesByName(pure))
                {
                    try { p.Kill(); } catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
        }

        // ==================== 周期兜底扫描 ====================

        private static void KillBlockedProcesses()
        {
            HashSet<string> names, paths;
            lock (_lock)
            {
                names = _blockedNames;
                paths = _blockedPaths;
            }

            if (names.Count == 0 && paths.Count == 0) return;

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    bool blocked = names.Contains(p.ProcessName);

                    if (!blocked && paths.Count > 0)
                    {
                        try
                        {
                            string? fp = p.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(fp)) blocked = paths.Contains(fp);
                        }
                        catch { }
                    }

                    if (blocked)
                    {
                        try { p.Kill(); } catch { }
                    }
                }
                catch { }
                finally { p.Dispose(); }
            }
        }
    }
}
