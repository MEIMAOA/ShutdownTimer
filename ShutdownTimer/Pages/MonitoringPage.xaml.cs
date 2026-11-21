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
    public sealed partial class MonitoringPage : Page
    {
        private readonly List<ProcessInfo> _all = new();
        private readonly ObservableCollection<ProcessInfo> _filtered = new();

        public MonitoringPage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            ProcHost.ItemsSource = _filtered;
            UptimeText.Text = Loc.T("开机时长：") + SystemInfoService.FormatUptime(SystemInfoService.GetUptime());
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var list = await Task.Run(() => SystemInfoService.GetProcesses());

            _all.Clear();
            _all.AddRange(list);
            ApplyFilter(SearchBox.Text);
        }

        private void ApplyFilter(string kw)
        {
            _filtered.Clear();

            IEnumerable<ProcessInfo> src = _all;
            if (!string.IsNullOrWhiteSpace(kw))
                src = src.Where(p => p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase));

            foreach (var p in src) _filtered.Add(p);

            CountText.Text = Loc.T("共") + " " + _filtered.Count + " " + Loc.T("个进程");
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void Search_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter(SearchBox.Text);

        private async void Kill_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.Tag is not ProcessInfo p) return;

            var dialog = new ContentDialog
            {
                Title = Loc.T("结束进程"),
                Content = $"确定要结束「{p.Name}」(PID {p.Id}) 吗？",
                PrimaryButtonText = Loc.T("结束"),
                CloseButtonText = Loc.T("取消"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            try
            {
                var proc = Process.GetProcessById(p.Id);
                proc.Kill();
                _all.Remove(p);
                _filtered.Remove(p);
            }
            catch (Exception ex)
            {
                var err = new ContentDialog
                {
                    Title = Loc.T("无法结束进程"),
                    Content = ex.Message,
                    CloseButtonText = Loc.T("确定"),
                    XamlRoot = this.XamlRoot
                };
                await err.ShowAsync();
            }
        }
    }
}