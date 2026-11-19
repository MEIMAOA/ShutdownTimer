using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 教育分支版进程守护：防止"禁止打开特定软件"因进程被任务管理器结束而失效。
    /// 主进程与守护进程互相监视，被结束后由另一方自动拉起；
    /// 只有通过"退出软件"正常退出才会一起退出（不重启）。
    /// </summary>
    public static class ProcessGuard
    {
        private const string MainMutexName = "ShutdownTimer_Main_Mutex";
        private const string GuardianMutexName = "ShutdownTimer_Guardian_Mutex";

        private static string GracefulFlagPath =>
            Path.Combine(Path.GetTempPath(), "ShutdownTimer_graceful_exit.flag");

        private static Mutex? _mainMutex;
        private static Mutex? _guardianMutex;

        /// <summary>当前进程是否为守护进程。</summary>
        public static bool IsGuardianMode { get; private set; }

        // ==================== 主进程 ====================

        /// <summary>主进程启动时调用（教育分支版才生效）。</summary>
        public static void StartAsMain()
        {
            if (!BuildInfo.IsEducation) return;

            try
            {
                ClearGracefulFlag();
                _mainMutex = new Mutex(false, MainMutexName);
                EnsureGuardianRunning();
            }
            catch { }
        }

        /// <summary>主进程周期调用：守护进程被结束后重新拉起它。</summary>
        public static void MainTick()
        {
            if (!BuildInfo.IsEducation) return;

            try
            {
                if (!IsMutexPresent(GuardianMutexName))
                    LaunchGuardian();
            }
            catch { }
        }

        // ==================== 守护进程 ====================

        /// <summary>守护进程启动时调用。</summary>
        public static void StartAsGuardian()
        {
            if (!BuildInfo.IsEducation) return;

            IsGuardianMode = true;
            try
            {
                _guardianMutex = new Mutex(false, GuardianMutexName);
            }
            catch { }
        }

        /// <summary>守护进程周期调用：主进程被结束后重新拉起它（正常退出则一起退出）。</summary>
        public static void GuardianTick()
        {
            try
            {
                if (IsMutexPresent(MainMutexName)) return;

                if (IsGracefulFlagSet())
                {
                    ClearGracefulFlag();
                    ExitApp();
                    return;
                }

                LaunchShutdownTimer("/silent");
            }
            catch { }
        }

        // ==================== 退出 ====================

        /// <summary>正常退出：标记为"正常退出"后退出（守护进程会随之退出，不再拉起）。</summary>
        public static void ExitGracefully()
        {
            if (BuildInfo.IsEducation)
            {
                try { SetGracefulFlag(); } catch { }
            }
            ExitApp();
        }

        // ==================== 内部实现 ====================

        private static void EnsureGuardianRunning()
        {
            if (!IsMutexPresent(GuardianMutexName))
                LaunchGuardian();
        }

        private static void LaunchGuardian()
        {
            try { LaunchShutdownTimer("--guardian"); } catch { }
        }

        private static void LaunchShutdownTimer(string args)
        {
            string exe = Path.Combine(AppContext.BaseDirectory, "ShutdownTimer.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }

        /// <summary>通过命名互斥体的存在与否判断对应进程是否存活。</summary>
        private static bool IsMutexPresent(string name)
        {
            bool createdNew;
            using var m = new Mutex(false, name, out createdNew);
            return !createdNew;
        }

        private static void SetGracefulFlag()
        {
            File.WriteAllText(GracefulFlagPath, DateTime.Now.ToString("o"));
        }

        private static void ClearGracefulFlag()
        {
            try { if (File.Exists(GracefulFlagPath)) File.Delete(GracefulFlagPath); } catch { }
        }

        private static bool IsGracefulFlagSet() => File.Exists(GracefulFlagPath);

        private static void ExitApp()
        {
            try { Application.Current.Exit(); } catch { }
        }
    }
}
