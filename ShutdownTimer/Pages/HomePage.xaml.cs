using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

        private async System.Threading.Tasks.Task LoadAsync()
        {
            StatusText.Text = "正在加载…";
            var list = await GitHubNewsService.FetchAsync();

            if (list.Count == 0)
            {
                StatusText.Text = "未能获取到资讯（网络不可用或请求超时）。点击「刷新」重试。";
                return;
            }

            StatusText.Text = $"共 {list.Count} 个仓库的最新资讯：";
            Render(list);
        }

        private void Render(List<RepoInfo> repos)
        {
            ContentPanel.Children.Clear();

            foreach (var repo in repos)
            {
                ContentPanel.Children.Add(BuildRepoCard(repo));
            }
        }

        private FrameworkElement BuildRepoCard(RepoInfo repo)
        {
            var border = new Border
            {
                Style = (Style)Application.Current.Resources["SettingsCardBorderStyle"]
            };

            var panel = new StackPanel { Spacing = 6, Padding = new Thickness(4) };

            // 名称行（可点击打开仓库）
            var nameBtn = new HyperlinkButton
            {
                Content = repo.FullName,
                HorizontalAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(0)
            };
            nameBtn.Click += (s, e) => OpenUrl(repo.HtmlUrl);
            panel.Children.Add(nameBtn);

            // 描述
            if (!string.IsNullOrWhiteSpace(repo.Description))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = repo.Description,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                });
            }

            // 统计
            string stats = $"★ {repo.Stars}";
            if (repo.Forks > 0) stats += $"   ⑂ {repo.Forks}";
            if (!string.IsNullOrWhiteSpace(repo.Language)) stats += $"   · {repo.Language}";
            if (repo.UpdatedAt.HasValue) stats += $"   · 更新于 {repo.UpdatedAt:yyyy-MM-dd}";
            panel.Children.Add(new TextBlock
            {
                Text = stats,
                Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
            });

            // 最新发布
            if (!string.IsNullOrWhiteSpace(repo.LatestReleaseTag))
            {
                string relText = $"最新版本：{repo.LatestReleaseTag}";
                if (!string.IsNullOrWhiteSpace(repo.LatestReleaseName))
                    relText += $"（{repo.LatestReleaseName}）";
                if (repo.ReleasedAt.HasValue)
                    relText += $" · {repo.ReleasedAt:yyyy-MM-dd}";

                panel.Children.Add(new TextBlock
                {
                    Text = relText,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["SystemAccentColor"]
                });

                if (!string.IsNullOrWhiteSpace(repo.ReleaseNotes))
                {
                    string notes = repo.ReleaseNotes;
                    if (notes.Length > 400) notes = notes.Substring(0, 400) + "…";
                    panel.Children.Add(new TextBlock
                    {
                        Text = notes,
                        TextWrapping = TextWrapping.Wrap,
                        Style = (Style)Application.Current.Resources["CaptionTextBlockStyle"],
                        Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                    });
                }
            }

            border.Child = panel;
            return border;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            ContentPanel.Children.Clear();
            _ = LoadAsync();
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
