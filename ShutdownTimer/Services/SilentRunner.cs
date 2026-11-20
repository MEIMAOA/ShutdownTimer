using System;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 静默模式：创建一个隐藏窗口用于维持消息循环和托盘图标，
    /// 并在进程内启动自定时调度器（关机/重启/休眠由程序自己计时）。
    /// </summary>
    public static class SilentRunner
    {
        private static Window? _holderWindow;
        private static DispatcherQueueTimer? _timer;

        public static void Run()
        {
            // 创建一个隐藏窗口，保证 WinUI 消息循环不退出、托盘图标可用
            _holderWindow = new Window
            {
                Title = "ShutdownTimer Background"
            };
            // 不调用 Activate()，窗口不会显示

            StartScheduleTimer(_holderWindow.DispatcherQueue);

            // 教育分支版：启动软件拦截
            try { SoftwareBlocker.Start(); } catch { }

            // 教育分支版：全局 Alt+L 退出（静默模式也要可用）
            if (BuildInfo.IsEducation)
            {
                try
                {
                    IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_holderWindow);
                    GlobalHotkey.Register(hwnd,
                        () => _holderWindow?.DispatcherQueue.TryEnqueue(() => ProcessGuard.ExitGracefully()));
                }
                catch { }
            }
        }

        private static void StartScheduleTimer(DispatcherQueue dispatcher)
        {
            try
            {
                _timer = dispatcher.CreateTimer();
                _timer.Interval = TimeSpan.FromSeconds(10);
                _timer.IsRepeating = true;
                _timer.Tick += (s, e) =>
                {
                    try { ShutdownScheduler.Tick(); } catch { }
                    try { SoftwareBlocker.Tick(); } catch { }
                    try { ProcessGuard.MainTick(); } catch { }
                };
                _timer.Start();
            }
            catch { }
        }
    }
}
