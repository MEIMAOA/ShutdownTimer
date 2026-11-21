using System;
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
            ShutdownTimer.Services.Localizer.ApplyOnLoaded(Content as Microsoft.UI.Xaml.FrameworkElement);

            PageTitleText.Text = Loc.T("主页");
            SubtitleText.Text = Loc.T("来自 GitHub 的最新资讯");
            RefreshButton.Content = Loc.T("刷新");

            _ = LoadAsync(false);
        }

        private async Task LoadAsync(bool forceRefresh)
        {
            SummaryText.Text = Loc.T("正在加载…");
            StatusText.Text = Loc.T("正在加载…");

            try
            {
                NewsResult result = await GitHubNewsService.FetchAsync(forceRefresh);

                if (result.Failed)
                {
                    SummaryText.Text = result.RateLimited ? Loc.T("接口限流") : Loc.T("加载失败");
                    StatusText.Text = result.RateLimited
                        ? Loc.T("GitHub 接口触发限流（未登录每小时约 60 次），请稍后再试；点击「刷新」重新拉取。")
                        : Loc.T("未能获取到资讯（网络不可用或请求超时）。点击「刷新」重试。");
                    return;
                }

                int totalStars = 0;
                foreach (var r in result.Repos) totalStars += r.Stars;

                string whenLabel = result.FromCache ? Loc.T("缓存于") : Loc.T("更新于");
                string timeText = result.FetchedAt.HasValue
                    ? result.FetchedAt.Value.ToString("HH:mm")
                    : "";
                string when = string.IsNullOrEmpty(timeText) ? "" : whenLabel + " " + timeText;
                string cacheNote = result.FromCache ? Loc.T("（缓存）") : "";

                SummaryText.Text = Loc.T("共") + " " + result.Repos.Count + " " + Loc.T("个仓库")
                    + " · " + Loc.T("总计") + " ★ " + totalStars + " · " + when + cacheNote;

                StatusText.Text = result.FromCache
                    ? Loc.T("当前为本地缓存数据（GitHub 接口可能被限流），可点击「刷新」尝试重新拉取。")
                    : "";

                RepoList.ItemsSource = result.Repos;
            }
            catch (Exception ex)
            {
                SummaryText.Text = Loc.T("加载失败");
                StatusText.Text = Loc.T("加载失败") + "：" + ex.Message;
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RepoList.ItemsSource = null;
            _ = LoadAsync(true);
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
