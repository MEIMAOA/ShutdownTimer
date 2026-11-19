using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class UpdatePage : Page
    {
        private UpdateCheckResult _update;
        private bool _loading;

        public UpdatePage()
        {
            this.InitializeComponent();

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersionText.Text = v != null ? $"v{v}" : "v2.0.0.2";

            _loading = true;
            var cfg = AppConfig.Load();
            bool edu = string.Equals(cfg.UpdateChannel, UpdateService.ChannelEducation, StringComparison.OrdinalIgnoreCase);
            OfficialChannelRadio.IsChecked = !edu;
            EducationChannelRadio.IsChecked = edu;
            _loading = false;
        }

        private void Channel_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;

            try
            {
                string channel = EducationChannelRadio.IsChecked == true
                    ? UpdateService.ChannelEducation
                    : UpdateService.ChannelOfficial;

                var c = AppConfig.Load();
                c.UpdateChannel = channel;
                c.Save();

                StatusText.Text = $"已切换到「{UpdateService.GetChannelDisplayName()}」渠道，点击上方「检查更新」即可检测。";
                InstallButton.Visibility = Visibility.Collapsed;
                DownloadProgress.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "正在检查更新…";
            InstallButton.Visibility = Visibility.Collapsed;
            DownloadProgress.Visibility = Visibility.Collapsed;

            _update = await UpdateService.CheckForUpdateAsync();

            if (_update == null)
            {
                StatusText.Text = "检查更新失败。";
                return;
            }

            if (!string.IsNullOrEmpty(_update.ErrorMessage))
            {
                StatusText.Text = $"检查失败：{_update.ErrorMessage}";
                return;
            }

            if (!_update.HasUpdate)
            {
                StatusText.Text = $"已是最新版本（v{_update.CurrentVersion}）。";
                return;
            }

            string sizeText = _update.FileSize > 0
                ? $"{_update.FileSize / 1048576.0:F1} MB"
                : "未知大小";

            if (_update.IsChannelSwitch)
            {
                StatusText.Text =
                    $"将切换到「{UpdateService.GetChannelDisplayName()}」渠道\n\n" +
                    $"当前版本：v{_update.CurrentVersion}\n" +
                    $"目标版本：{_update.LatestVersion}\n" +
                    $"包大小：{sizeText}\n\n" +
                    $"更新内容：\n{_update.ReleaseNotes}";
            }
            else
            {
                StatusText.Text =
                    $"发现新版本 {_update.LatestVersion}\n\n" +
                    $"当前版本：v{_update.CurrentVersion}\n" +
                    $"最新版本：{_update.LatestVersion}\n" +
                    $"包大小：{sizeText}\n\n" +
                    $"更新内容：\n{_update.ReleaseNotes}";
            }

            InstallButton.Visibility = Visibility.Visible;
            InstallButton.IsEnabled = true;
        }

        private async void InstallUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (_update == null || string.IsNullOrEmpty(_update.DownloadUrl)) return;

            InstallButton.IsEnabled = false;
            DownloadProgress.Visibility = Visibility.Visible;
            DownloadProgress.Value = 0;

            try
            {
                StatusText.Text = "正在下载更新包…";

                var progress = new Progress<double>(p =>
                {
                    DownloadProgress.Value = p;
                    StatusText.Text = $"正在下载… {p:F1}%";
                });

                string zipPath = await UpdateService.DownloadUpdateAsync(
                    _update.DownloadUrl, progress, CancellationToken.None);

                if (!string.IsNullOrEmpty(_update.Sha256))
                {
                    StatusText.Text = "正在校验…";
                    bool ok = await Task.Run(() => UpdateService.VerifySha256(zipPath, _update.Sha256));
                    if (!ok)
                    {
                        try { File.Delete(zipPath); } catch { }
                        StatusText.Text = "校验失败，更新包可能已损坏。";
                        InstallButton.IsEnabled = true;
                        DownloadProgress.Visibility = Visibility.Collapsed;
                        return;
                    }
                }

                StatusText.Text = "正在启动更新程序…";
                await Task.Delay(400);

                // ★ 用 try-catch 包裹，把错误显示出来
                try
                {
                    UpdateService.LaunchUpdater(zipPath);
                }
                catch (Exception exLaunch)
                {
                    StatusText.Text =
                        $"无法启动更新程序：\n{exLaunch.Message}\n\n" +
                        $"请查看日志：\n%TEMP%\\ShutdownTimer_Update.log";
                    InstallButton.IsEnabled = true;
                    DownloadProgress.Visibility = Visibility.Collapsed;
                    return;
                }

                await Task.Delay(600);
                Application.Current.Exit();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"更新失败：{ex.Message}";
                InstallButton.IsEnabled = true;
                DownloadProgress.Visibility = Visibility.Collapsed;
            }
        }
    }
}