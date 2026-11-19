using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 关机/重启/休眠/锁定工具。
    /// 关机与重启直接执行即时指令（shutdown.exe /s|/r /f /t 0），
    /// 不再使用倒计时，因此不会弹出"Windows 即将注销"的提示。
    /// 定时由程序自身的计时器负责（见 ShutdownScheduler.Tick）。
    /// </summary>
    public static class ShutdownHelper
    {
        // ============================================================
        // Win32 API（仅用于休眠 / 睡眠 / 锁定）
        // ============================================================

        [DllImport("powrprof.dll", SetLastError = true)]
        private static extern bool SetSuspendState(
            bool bHibernate,
            bool bForce,
            bool bWakeupEventsDisabled);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        /// <summary>shutdown.exe 的完整路径（System32，不受 PATH 影响）。</summary>
        private static string ShutdownExe =>
            Path.Combine(Environment.SystemDirectory, "shutdown.exe");

        // ============================================================
        // 对外 API
        // ============================================================

        /// <summary>立即关机（强制关闭应用，无倒计时提示）。</summary>
        public static bool ShutdownNow() => RunShutdown("/s /f /t 0");

        /// <summary>立即重启（强制关闭应用，无倒计时提示）。</summary>
        public static bool RestartNow() => RunShutdown("/r /f /t 0");

        /// <summary>休眠（保存内存到硬盘，功耗极低）。</summary>
        public static bool Hibernate()
        {
            try
            {
                return SetSuspendState(true, false, false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>睡眠（内存保持供电，恢复快）。</summary>
        public static bool Sleep()
        {
            try
            {
                return SetSuspendState(false, false, false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>锁定屏幕。</summary>
        public static bool Lock()
        {
            try
            {
                return LockWorkStation();
            }
            catch
            {
                return false;
            }
        }

        // ============================================================
        // 内部实现
        // ============================================================

        private static bool RunShutdown(string arguments)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ShutdownExe,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
