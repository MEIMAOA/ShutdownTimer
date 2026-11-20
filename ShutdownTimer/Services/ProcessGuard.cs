using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using Microsoft.UI.Xaml;

namespace ShutdownTimer.Services
{
    /// <summary>
    /// 教育分支版进程守护：防止"禁止打开特定软件"因进程被任务管理器结束而失效。
    /// 主进程与守护进程互相监视（通过 WMI 枚举命令行判断对方是否存活），
    /// 被结束后由另一方自动拉起；只有"退出软件"正常退出才会一起退出（不重启）。
    /// </summary>
    public static class ProcessGuard
    {
        private const string ExeName = "ShutdownTimer.exe";
        private const string GuardianArg = "--guardian";

        private static string GracefulFlagPath =>
            Path.Combine(Path.GetTempPath(), "ShutdownTimer_graceful_exit.flag");

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
                if (!IsGuardianRunning())
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
        }

        /// <summary>守护进程周期调用：主进程被结束后重新拉起它（正常退出则一起退出）。</summary>
        public static void GuardianTick()
        {
            try
            {
                if (IsMainRunning()) return;

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
            if (!IsGuardianRunning())
                LaunchGuardian();
        }

        private static void LaunchGuardian()
        {
            try { LaunchShutdownTimer(GuardianArg); } catch { }
        }

        private static void LaunchShutdownTimer(string args)
        {
            string exe = Path.Combine(AppContext.BaseDirectory, ExeName);
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = AppContext.BaseDirectory
            });
        }

        /// <summary>是否存在一个非守护进程的 ShutdownTimer.exe（即主进程）。</summary>
        private static bool IsMainRunning()
        {
            foreach (string cmd in EnumerateCommandLines())
            {
                if (cmd.IndexOf(GuardianArg, StringComparison.OrdinalIgnoreCase) < 0)
                    return true;
            }
            return false;
        }

        /// <summary>是否存在守护进程（命令行含 --guardian）。</summary>
        private static bool IsGuardianRunning()
        {
            foreach (string cmd in EnumerateCommandLines())
            {
                if (cmd.IndexOf(GuardianArg, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        /// <summary>枚举所有 ShutdownTimer.exe 的命令行。</summary>
        private static System.Collections.Generic.List<string> EnumerateCommandLines()
        {
            var result = new System.Collections.Generic.List<string>();
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE Name = '{ExeName}'");
                foreach (ManagementObject mo in searcher.Get())
                {
                    string cmd = mo["CommandLine"]?.ToString() ?? "";
                    if (cmd.Length > 0) result.Add(cmd);
                }
            }
            catch { }
            return result;
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
