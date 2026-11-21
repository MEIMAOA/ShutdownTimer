using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class CleanupPage : Page
    {
        private readonly ObservableCollection<LargeFile> _files = new();

        public CleanupPage()
        {
            this.InitializeComponent();
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);
            LargeFileHost.ItemsSource = _files;
            ScanPathText.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _ = UpdateTempSizeAsync();
        }

        private async Task UpdateTempSizeAsync()
        {
            TempSizeText.Text = Loc.T("计算中…");
            var size = await Task.Run(() => CleanupService.GetDirectorySize(Path.GetTempPath()));
            TempSizeText.Text = Loc.T("当前占用") + " " + SystemInfoService.FormatBytes(size);
        }

        private async void CleanTemp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = Loc.T("清理临时文件"),
                Content = Loc.T("将删除 %TEMP% 和 Windows\\Temp 中的临时文件，确定继续？"),
                PrimaryButtonText = Loc.T("清理"),
                CloseButtonText = Loc.T("取消"),
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            var (count, freed) = await Task.Run(() => CleanupService.CleanTempFolders());

            var result = new ContentDialog
            {
                Title = Loc.T("清理完成"),
                Content = Loc.T("删除了") + " " + count + " " + Loc.T("个文件，释放") + " " + SystemInfoService.FormatBytes(freed),
                CloseButtonText = Loc.T("确定"),
                XamlRoot = this.XamlRoot
            };
            await result.ShowAsync();

            _ = UpdateTempSizeAsync();
        }

        private async void EmptyRecycle_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = Loc.T("清空回收站"),
                Content = Loc.T("确定要清空回收站吗？此操作不可撤销。"),
                PrimaryButtonText = Loc.T("清空"),
                CloseButtonText = Loc.T("取消"),
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            await Task.Run(() => CleanupService.EmptyRecycleBin());
        }

        private async void ScanLargeFiles_Click(object sender, RoutedEventArgs e)
        {
            _files.Clear();
            ScanProgress.Visibility = Visibility.Visible;

            long minBytes = MinSizeCombo.SelectedIndex switch
            {
                0 => 100L * 1024 * 1024,
                1 => 500L * 1024 * 1024,
                2 => 1024L * 1024 * 1024,
                _ => 5L * 1024 * 1024 * 1024
            };

            string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            ScanStatusText.Text = "扫描中…";

            var result = await Task.Run(() => CleanupService.ScanLargeFiles(root, minBytes, 100));

            foreach (var f in result) _files.Add(f);

            ScanProgress.Visibility = Visibility.Collapsed;
            ScanStatusText.Text = $"找到 {result.Count} 个大于 {SystemInfoService.FormatBytes(minBytes)} 的文件";
        }
    }
}