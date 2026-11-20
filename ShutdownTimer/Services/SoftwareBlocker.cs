using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 教育分支版专属：软件管控。
    /// - 通过 WMI Win32_ProcessStartTrace 监听进程启动，进程一启动即被检查（即时拦截）；
    /// - 周期扫描兜底（含完整路径匹配）。
    /// 支持：完全禁止、每日限时、允许使用的时间段。
    /// </summary>
    public static class SoftwareBlocker
    {
        private const int TickSeconds = 10;

        private static ManagementEventWatcher? _watcher;
        private static DateTime _lastTick = DateTime.Now;

        /// <summary>启动拦截（应用启动时调用一次）。</summary>
        public static void Start()
        {
            if (!BuildInfo.IsEducation) return;

            StartWatcher();
        }

        /// <summary>周期调用（兜底扫描 + 限时/时间段 + 使用时长累加）。</summary>
        public static void Tick()
        {
            if (!BuildInfo.IsEducation) return;

            try
            {
                var now = DateTime.Now;
                int deltaSeconds = (int)(now - _lastTick).TotalSeconds;
                _lastTick = now;
                if (deltaSeconds <= 0 || deltaSeconds > 120) deltaSeconds = TickSeconds;

                var c = AppConfig.Load();
                if (c.ManagedApps == null || c.ManagedApps.Count == 0) return;

                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        ManagedApp? rule = FindRule(p, c.ManagedApps);
                        if (rule == null) continue;

                        if (IsAllowed(rule, p.ProcessName, now))
                        {
                            UsageTracker.AddSeconds(p.ProcessName, deltaSeconds);
                        }
                        else
                        {
                            try { p.Kill(); } catch { }
                        }
                    }
                    catch { }
                    finally { p.Dispose(); }
                }
            }
            catch { }
        }

        // ==================== 规则匹配 ====================

        private static ManagedApp? FindRule(Process p, List<ManagedApp> rules)
        {
            foreach (var rule in rules)
            {
                string rp = (rule.Process ?? "").Trim();
                if (rp.Length == 0) continue;

                bool match;
                if (rp.IndexOf('\\') >= 0 || rp.IndexOf('/') >= 0)
                {
                    string? fp = null;
                    try { fp = p.MainModule?.FileName; } catch { }
                    match = !string.IsNullOrEmpty(fp)
                        && string.Equals(fp, Path.GetFullPath(rp), StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    match = string.Equals(p.ProcessName, StripExe(rp), StringComparison.OrdinalIgnoreCase);
                }

                if (match) return rule;
            }
            return null;
        }

        private static bool IsAllowed(ManagedApp rule, string processName, DateTime now)
        {
            // 完全禁止
            if (rule.MaxMinutesPerDay == 0) return false;

            // 时间段：非空且当前不在窗口内 → 不允许
            if (!string.IsNullOrWhiteSpace(rule.AllowedStart)
                && !string.IsNullOrWhiteSpace(rule.AllowedEnd))
            {
                if (!IsWithinWindow(now, rule.AllowedStart, rule.AllowedEnd))
                    return false;
            }

            // 每日限时（>0 表示限时；-1 表示不限时长）
            if (rule.MaxMinutesPerDay > 0)
            {
                int used = UsageTracker.GetSecondsToday(processName);
                if (used >= rule.MaxMinutesPerDay * 60) return false;
            }

            return true;
        }

        private static bool IsWithinWindow(DateTime now, string start, string end)
        {
            if (!TryParseTime(start, out var st) || !TryParseTime(end, out var et))
                return true; // 解析失败则放行

            var t = now.TimeOfDay;
            if (st <= et)
                return t >= st && t <= et;
            return t >= st || t <= et; // 跨天时间段，如 22:00-06:00
        }

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
                if (string.IsNullOrEmpty(processName)) return;

                int pid = int.TryParse(pidStr, out int parsed) ? parsed : -1;

                var c = AppConfig.Load();
                if (c.ManagedApps == null || c.ManagedApps.Count == 0) return;

                var now = DateTime.Now;
                foreach (var rule in c.ManagedApps)
                {
                    if (!NameMatches(processName, rule.Process))
                        continue;

                    if (!IsAllowed(rule, Path.GetFileNameWithoutExtension(processName), now))
                        KillProcess(pid, processName);
                    return;
                }
            }
            catch { }
        }

        private static bool NameMatches(string processName, string ruleProcess)
        {
            string rp = (ruleProcess ?? "").Trim();
            if (rp.Length == 0) return false;

            if (rp.IndexOf('\\') >= 0 || rp.IndexOf('/') >= 0)
            {
                return processName.IndexOf('\\') >= 0
                    && string.Equals(processName, rp, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(
                Path.GetFileNameWithoutExtension(processName),
                StripExe(rp),
                StringComparison.OrdinalIgnoreCase);
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

        // ==================== 工具 ====================

        private static string StripExe(string s)
            => s.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? s.Substring(0, s.Length - 4)
                : s;

        private static bool TryParseTime(string s, out TimeSpan time)
        {
            time = TimeSpan.Zero;
            var parts = (s ?? "").Trim().Split(':');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out int h) || !int.TryParse(parts[1], out int m)) return false;
            if (h < 0 || h > 23 || m < 0 || m > 59) return false;
            time = new TimeSpan(h, m, 0);
            return true;
        }
    }
}
