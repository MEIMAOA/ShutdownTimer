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

                // 关机（支持多个时间段）
                if (c.Enabled && c.ShutdownTimes != null && c.ShutdownTimes.Contains(hhmm))
                {
                    if (TryFire($"shutdown:{now:yyyyMMdd}:{hhmm}"))
                        ShutdownHelper.ShutdownNow();
                    return;
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
