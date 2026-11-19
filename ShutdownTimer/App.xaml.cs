using System;
using System.IO;
using System.Linq;
using ShutdownTimer.Services;

namespace ShutdownTimer
{
    public partial class App : Microsoft.UI.Xaml.Application
    {
        private Microsoft.UI.Xaml.Window? _window;

        public App()
        {
            this.InitializeComponent();

            // ★ 捕获 XAML 层的未处理异常
            this.UnhandledException += (s, e) =>
            {
                try
                {
                    string logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ShutdownTimer_xaml_crash.log");
                    File.WriteAllText(logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n" +
                        $"系统版本: {Environment.OSVersion}\n" +
                        $"XAML 异常: {e.Message}\n\n" +
                        $"详细: {e.Exception}");
                }
                catch { }
            };
        }

        protected override void OnLaunched(
            Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            var cmdArgs = Environment.GetCommandLineArgs();

            bool silent = cmdArgs.Any(a => a.Equals("/silent", StringComparison.OrdinalIgnoreCase));
            bool guardian = cmdArgs.Any(a => a.Equals("--guardian", StringComparison.OrdinalIgnoreCase));

            // 守护进程模式（教育分支版）：只运行看门狗，不显示主窗口
            if (guardian)
            {
                ProcessGuard.StartAsGuardian();
                GuardianRunner.Run();
                return;
            }

            // 主进程：教育分支版启动进程守护
            ProcessGuard.StartAsMain();

            if (silent)
            {
                SilentRunner.Run();
            }
            else
            {
                _window = new MainWindow();
                _window.Activate();
            }
        }
    }
}