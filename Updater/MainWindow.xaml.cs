using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace ShutdownTimer.Updater
{
    public sealed partial class MainWindow : Window
    {
        private readonly string _appDir;
        private readonly string _zipPath;
        private readonly string _exeName;
        private readonly string _tempDir;
        private readonly string _version;
        private readonly string _notesFile;
        private readonly string _appName;
        private UpdateEngine? _engine;
        private bool _completed;
        private double _trackWidth = 552;

        public MainWindow(string[] args)
        {
            App.Log("MainWindow 构造函数进入");
            this.InitializeComponent();
            App.Log("InitializeComponent 完成");

            try { this.AppWindow.Resize(new SizeInt32(640, 480)); } catch { }

            // ★ 自绘标题栏
            try
            {
                this.ExtendsContentIntoTitleBar = true;
                this.SetTitleBar(TitleBarRegion);
            }
            catch { }

            _appDir = GetArg(args, "--app-dir") ?? "";
            _zipPath = GetArg(args, "--zip") ?? "";
            _exeName = GetArg(args, "--exe") ?? "ShutdownTimer.exe";
            _tempDir = GetArg(args, "--temp-dir") ?? "";
            _version = GetArg(args, "--version") ?? "";
            _notesFile = GetArg(args, "--notes-file") ?? "";
            _appName = GetArg(args, "--app-name") ?? "时序";

            App.Log($"参数: appDir={_appDir}, zip={_zipPath}, exe={_exeName}, version={_version}");

            // 顶部应用名
            TxtAppName.Text = _appName;
            TxtTitleBar.Text = _appName + " · 更新";
            try { this.Title = _appName + " · 更新"; } catch { }

            // 正在更新到新版本 X
            TxtStatus.Text = string.IsNullOrWhiteSpace(_version)
                ? "正在更新到新版本"
                : "正在更新到新版本 " + _version;

            // 更新内容
            TxtNotes.Text = LoadNotes(_notesFile);
            if (string.IsNullOrWhiteSpace(TxtNotes.Text))
                TxtNotes.Text = "暂无更新说明";

            this.Activated += MainWindow_Activated;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated)
            {
                this.Activated -= MainWindow_Activated;
                _ = RunUpdateAsync();
            }
        }

        private static string LoadNotes(string notesFile)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(notesFile) && File.Exists(notesFile))
                    return File.ReadAllText(notesFile).Trim();
            }
            catch { }
            return "";
        }

        private async Task RunUpdateAsync()
        {
            App.Log("RunUpdateAsync 触发");

            if (string.IsNullOrWhiteSpace(_appDir)) { ShowError("缺少参数: --app-dir"); return; }
            if (string.IsNullOrWhiteSpace(_zipPath)) { ShowError("缺少参数: --zip"); return; }
            if (!File.Exists(_zipPath)) { ShowError($"更新包不存在:\n{_zipPath}"); return; }
            if (!Directory.Exists(_appDir)) { ShowError($"应用目录不存在:\n{_appDir}"); return; }

            App.Log("参数校验通过，开始更新");

            BottomButtons.Visibility = Visibility.Collapsed;
            TxtError.Visibility = Visibility.Collapsed;
            TxtError.Text = "";
            ProgressFill.Width = 0;
            TxtPercent.Text = "0%";
            TxtProgressLabel.Text = "正在准备...";

            _engine = new UpdateEngine();
            _engine.ProgressChanged += OnProgressChanged;

            bool success = false;
            string? error = null;

            try
            {
                success = await Task.Run(() => _engine.Run(_appDir, _zipPath, _exeName));
                error = _engine.LastError;
                App.Log($"更新结果: success={success}, error={error}");
            }
            catch (Exception ex)
            {
                App.Log($"!! 更新异常: {ex}");
                error = ex.Message;
            }

            if (success)
            {
                _completed = true;
                ScheduleTempDirCleanup(_tempDir);
                await Task.Delay(1400);
                Microsoft.UI.Xaml.Application.Current.Exit();
            }
            else
            {
                ShowError(error ?? "未知错误");
            }
        }

        private void OnProgressChanged(UpdateProgress p)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                App.Log($"[{p.Percentage:F0}%] {p.Message}");

                TxtProgressLabel.Text = p.Message ?? "";

                if (p.Percentage >= 0)
                {
                    double targetWidth = _trackWidth * Math.Min(100, p.Percentage) / 100.0;
                    ProgressFill.Width = targetWidth;
                    TxtPercent.Text = $"{p.Percentage:F0}%";
                }
                else
                {
                    TxtPercent.Text = "处理中...";
                }
            });
        }

        private static void ScheduleTempDirCleanup(string tempDir)
        {
            if (string.IsNullOrWhiteSpace(tempDir)) return;
            if (!Directory.Exists(tempDir)) return;

            try
            {
                string batPath = Path.Combine(
                    Path.GetTempPath(),
                    "cleanup_updater_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".bat");

                string script =
                    "@echo off\r\n" +
                    "ping 127.0.0.1 -n 3 > nul\r\n" +
                    $"rmdir /s /q \"{tempDir}\" 2>nul\r\n" +
                    $"del /f /q \"%~f0\" 2>nul\r\n";

                File.WriteAllText(batPath, script, System.Text.Encoding.Default);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch { }
        }

        private void ShowError(string message)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                App.Log($"显示错误: {message}");
                TxtStatus.Text = "更新失败";
                TxtProgressLabel.Text = "";
                TxtPercent.Text = "";
                TxtError.Text = message;
                TxtError.Visibility = Visibility.Visible;
                BottomButtons.Visibility = Visibility.Visible;
            });
        }

        private async void BtnRetry_Click(object sender, RoutedEventArgs e) => await RunUpdateAsync();

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            _completed = true;
            Microsoft.UI.Xaml.Application.Current.Exit();
        }

        // ==================== 自绘标题栏按钮 ====================

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter p)
                    p.Minimize();
            }
            catch { }
        }

        private void BtnCloseTitle_Click(object sender, RoutedEventArgs e)
        {
            _completed = true;
            Microsoft.UI.Xaml.Application.Current.Exit();
        }

        private static string? GetArg(string[] args, string key)
        {
            if (args == null) return null;
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return null;
        }
    }
}
