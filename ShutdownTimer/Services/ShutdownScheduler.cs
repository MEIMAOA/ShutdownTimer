using System;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 自定时调度器：由程序自己的计时器周期调用 <see cref="Tick"/>，
    /// 到设定时间后直接执行关机 / 重启 / 休眠指令（无 Windows 倒计时提示）。
    /// </summary>
    public static class ShutdownScheduler
    {
        private static readonly object _lock = new();
        private static string? _lastFiredKey;

        /// <summary>
        /// 每 tick 检查一次当前时间，命中某个设定时间点就执行对应动作。
        /// 建议每 10~30 秒调用一次。
        /// </summary>
        public static void Tick()
        {
            try
            {
                var c = AppConfig.Load();
                var now = DateTime.Now;
                string hhmm = $"{now.Hour:D2}:{now.Minute:D2}";

                // 关机（支持多个时间段 + 按星期几）
                if (c.Enabled && c.ShutdownTimes != null)
                {
                    foreach (var entry in c.ShutdownTimes)
                    {
                        if (MatchesShutdownEntry(entry, now))
                        {
                            if (TryFire($"shutdown:{now:yyyyMMdd}:{now.DayOfWeek}:{hhmm}"))
                                ShutdownHelper.ShutdownNow();
                            return;
                        }
                    }
                }

                // 重启
                if (c.AutoRestartEnabled &&
                    now.Hour == c.AutoRestartHour && now.Minute == c.AutoRestartMinute)
                {
                    if (TryFire($"restart:{now:yyyyMMdd}:{hhmm}"))
                        ShutdownHelper.RestartNow();
                    return;
                }

                // 休眠
                if (c.AutoHibernateEnabled &&
                    now.Hour == c.AutoHibernateHour && now.Minute == c.AutoHibernateMinute)
                {
                    if (TryFire($"hibernate:{now:yyyyMMdd}:{hhmm}"))
                        ShutdownHelper.Hibernate();
                    return;
                }
            }
            catch { }
        }

        /// <summary>
        /// 判断某条关机时间是否匹配当前时刻。
        /// 格式："HH:mm"（每天）或 "D:HH:mm"（D=0..6，周日=0）。
        /// </summary>
        private static bool MatchesShutdownEntry(string entry, DateTime now)
        {
            if (!TryParseEntry(entry, out int? day, out int hour, out int minute))
                return false;

            if (day.HasValue && (int)now.DayOfWeek != day.Value)
                return false;

            return now.Hour == hour && now.Minute == minute;
        }

        private static bool TryParseEntry(string s, out int? day, out int hour, out int minute)
        {
            day = null;
            hour = 0;
            minute = 0;

            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Trim().Split(':');

            if (parts.Length == 3)
            {
                if (!int.TryParse(parts[0], out int d) || d < 0 || d > 6) return false;
                day = d;
                return int.TryParse(parts[1], out hour) && int.TryParse(parts[2], out minute)
                    && hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
            }

            if (parts.Length == 2)
            {
                return int.TryParse(parts[0], out hour) && int.TryParse(parts[1], out minute)
                    && hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59;
            }

            return false;
        }

        /// <summary>同一分钟内只触发一次，避免重复执行。</summary>
        private static bool TryFire(string key)
        {
            lock (_lock)
            {
                if (_lastFiredKey == key) return false;
                _lastFiredKey = key;
                return true;
            }
        }
    }
}
