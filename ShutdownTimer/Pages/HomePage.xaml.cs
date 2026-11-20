using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ShutdownTimer.Services;

namespace ShutdownTimer.Pages
{
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            StatusText.Text = "正在加载…";

            try
            {
                List<RepoInfo> list = await GitHubNewsService.FetchAsync();

                if (list.Count == 0)
                {
                    StatusText.Text = "未能获取到资讯（网络不可用或请求超时）。点击「刷新」重试。";
                    return;
                }

                StatusText.Text = $"共 {list.Count} 个仓库的最新资讯：";
                RepoList.ItemsSource = list;
            }
            catch (Exception ex)
            {
                StatusText.Text = $"加载失败：{ex.Message}";
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RepoList.ItemsSource = null;
            _ = LoadAsync();
        }

        private void RepoLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is RepoInfo repo)
                OpenUrl(repo.HtmlUrl);
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch { }
        }
    }
}
