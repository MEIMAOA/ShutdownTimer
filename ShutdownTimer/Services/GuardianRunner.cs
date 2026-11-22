using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Services
{
    /// <summary>守护进程的运行载体：隐藏窗口 + 周期检查主进程存活。</summary>
    public static class GuardianRunner
    {
        private static Window? _holder;
        private static DispatcherQueueTimer? _timer;

        public static void Run()
        {
            // 隐藏窗口，维持消息循环（不调用 Activate，并显式强制隐藏）
            _holder = new Window { Title = "ShutdownTimer Guardian" };
            HiddenWindowHelper.EnsureHidden(_holder);

            try
            {
                _timer = _holder.DispatcherQueue.CreateTimer();
                _timer.Interval = TimeSpan.FromSeconds(3);
                _timer.IsRepeating = true;
                _timer.Tick += (s, e) =>
                {
                    try { ProcessGuard.GuardianTick(); } catch { }
                };
                _timer.Start();
            }
            catch { }
        }
    }
}
