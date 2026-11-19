using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class AppsPage : Page
    {
        private readonly List<InstalledApp> _allApps = new();
        private readonly ObservableCollection<InstalledApp> _filtered = new();

        public AppsPage()
        {
            this.InitializeComponent();
            ItemsHost.ItemsSource = _filtered;
            _ = LoadAppsAsync();
        }

        private async Task LoadAppsAsync()
        {
            CountText.Text = "加载中…";
            _allApps.Clear();
            _filtered.Clear();

            // 1. 后台线程读注册表
            var apps = await Task.Run(() => InstalledAppsManager.GetAll());

            // 2. UI 线程：逐个提取图标并构建 BitmapImage
            foreach (var app in apps)
            {
                if (!string.IsNullOrWhiteSpace(app.IconPath))
                {
                    try
                    {
                        // ★ 改用新方法名
                        app.Icon = IconHelper.LoadIconFromAnyFile(app.IconPath);
                    }
                    catch
                    {
                        app.Icon = null;
                    }
                }

                _allApps.Add(app);
            }

            ApplyFilter(SearchBox.Text);
        }

        private void ApplyFilter(string keyword)
        {
            _filtered.Clear();

            IEnumerable<InstalledApp> source = _allApps;
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                source = source.Where(a =>
                    a.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    a.Publisher.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var app in source)
                _filtered.Add(app);

            CountText.Text = $"共 {_filtered.Count} 项";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadAppsAsync();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchBox.Text);
        }

        // ==================== 卸载 ====================

        private async void Uninstall_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not InstalledApp app) return;

            if (string.IsNullOrWhiteSpace(app.UninstallString))
            {
                await ShowMessage("无法卸载",
                    $"「{app.Name}」没有提供卸载信息，请通过系统「设置 → 应用」手动卸载。");
                return;
            }

            var confirm = new ContentDialog
            {
                Title = "卸载应用",
                Content = $"确定要卸载「{app.Name}」吗？\n\n系统会启动该应用的卸载程序，请按提示完成操作。",
                PrimaryButtonText = "卸载",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirm.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{app.UninstallString}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch (Exception ex)
            {
                await ShowMessage("卸载失败", ex.Message);
            }
        }

        private async Task ShowMessage(string title, string content)
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}