using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ShutdownTimer.Services
{
    /// <summary>GitHub 仓库资讯条目。</summary>
    public class RepoInfo
    {
        public string FullName { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int Stars { get; set; }
        public int Forks { get; set; }
        public string Language { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public string OwnerAvatarUrl { get; set; } = "";
        public string? LatestReleaseTag { get; set; }
        public string? LatestReleaseName { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }

        // ===== 供 XAML 绑定的展示用属性 =====

        private static readonly Dictionary<string, string> LanguageColors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["C#"] = "#178600",
                ["C++"] = "#f34b7d",
                ["C"] = "#555555",
                ["Python"] = "#3572A5",
                ["JavaScript"] = "#f1e05a",
                ["TypeScript"] = "#3178c6",
                ["Java"] = "#b07219",
                ["Go"] = "#00ADD8",
                ["Rust"] = "#dea584",
                ["Kotlin"] = "#A97BFF",
                ["Swift"] = "#F05138",
                ["Ruby"] = "#701516",
                ["PHP"] = "#4F5D95",
                ["HTML"] = "#e34c26",
                ["CSS"] = "#563d7c",
                ["Shell"] = "#89e051",
                ["Batchfile"] = "#C1F12E",
                ["PowerShell"] = "#012456",
                ["XAML"] = "#0060ac"
            };

        public BitmapImage? OwnerAvatarImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OwnerAvatarUrl)) return null;
                try { return new BitmapImage(new Uri(OwnerAvatarUrl)); }
                catch { return null; }
            }
        }

        public SolidColorBrush LanguageColorBrush
        {
            get
            {
                string hex = LanguageColors.TryGetValue(Language ?? "", out var c) ? c : "#808080";
                return new SolidColorBrush(ParseHexColor(hex));
            }
        }

        public string UpdatedText => UpdatedAt.HasValue ? UpdatedAt.Value.ToString("yyyy-MM-dd") : "";

        public string ReleaseBadgeText =>
            string.IsNullOrWhiteSpace(LatestReleaseTag) ? "" : $"最新版本 {LatestReleaseTag}";

        public string ReleaseDateText =>
            ReleasedAt.HasValue ? ReleasedAt.Value.ToString("yyyy-MM-dd") : "";

        public Visibility ReleaseVisibility =>
            string.IsNullOrWhiteSpace(LatestReleaseTag) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility NotesVisibility =>
            string.IsNullOrWhiteSpace(CleanNotes) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DescriptionVisibility =>
            string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>去掉 Markdown 标记 / 隐藏注释后的发布说明摘要。</summary>
        public string CleanNotes
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ReleaseNotes)) return "";

                var sb = new StringBuilder();
                foreach (var raw in ReleaseNotes!.Split('\n'))
                {
                    string l = raw.Trim();
                    if (l.Length == 0) continue;
                    if (l.Contains("<!--") || l.Contains("-->")) continue;

                    l = l.TrimStart('#', '>', '-', '*', '•', ' ');
                    l = Regex.Replace(l, @"!\[.*?\]\(.*?\)", "");
                    l = Regex.Replace(l, @"\[(.*?)\]\(.*?\)", "$1");
                    l = l.Replace("**", "").Replace("`", "").Replace("~~", "").Trim();

                    if (l.Length == 0) continue;
                    sb.AppendLine(l);
                }

                string result = sb.ToString().Trim();
                return result.Length > 220 ? result.Substring(0, 220) + "…" : result;
            }
        }

        private static Windows.UI.Color ParseHexColor(string hex)
        {
            try
            {
                if (hex.Length == 7 && hex[0] == '#')
                {
                    return Windows.UI.Color.FromArgb(255,
                        Convert.ToByte(hex.Substring(1, 2), 16),
                        Convert.ToByte(hex.Substring(3, 2), 16),
                        Convert.ToByte(hex.Substring(5, 2), 16));
                }
            }
            catch { }

            return Windows.UI.Color.FromArgb(255, 128, 128, 128);
        }
    }

    /// <summary>主页资讯源：从指定的 GitHub 仓库获取信息。</summary>
    public static class GitHubNewsService
    {
        private const string Owner = "MEIMAOA";

        private static readonly string[] RepoNames =
        {
            "ShutdownTimer-Edu",
            "ShutdownTimer",
            "SysKit",
            "SnakeLauncher",
            "SnakeGame"
        };

        private static readonly HttpClient _http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ShutdownTimer/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            return client;
        }

        public static async Task<List<RepoInfo>> FetchAsync()
        {
            var result = new List<RepoInfo>();

            foreach (var repo in RepoNames)
            {
                try
                {
                    var info = await FetchRepoAsync(repo);
                    if (info != null) result.Add(info);
                }
                catch { }
            }

            return result;
        }

        private static async Task<RepoInfo?> FetchRepoAsync(string repo)
        {
            var repoData = await GetJsonAsync($"https://api.github.com/repos/{Owner}/{repo}");
            if (repoData == null) return null;

            var info = new RepoInfo
            {
                FullName = GetString(repoData.Value, "full_name") ?? $"{Owner}/{repo}",
                Name = GetString(repoData.Value, "name") ?? repo,
                Description = GetString(repoData.Value, "description") ?? "",
                Stars = GetInt(repoData.Value, "stargazers_count"),
                Forks = GetInt(repoData.Value, "forks_count"),
                Language = GetString(repoData.Value, "language") ?? "",
                HtmlUrl = GetString(repoData.Value, "html_url") ?? $"https://github.com/{Owner}/{repo}",
                OwnerAvatarUrl = GetNestedString(repoData.Value, "owner", "avatar_url") ?? ""
            };
            if (DateTime.TryParse(GetString(repoData.Value, "updated_at"), out var upd))
                info.UpdatedAt = upd;

            try
            {
                var relData = await GetJsonAsync($"https://api.github.com/repos/{Owner}/{repo}/releases/latest");
                if (relData != null)
                {
                    info.LatestReleaseTag = GetString(relData.Value, "tag_name");
                    info.LatestReleaseName = GetString(relData.Value, "name");
                    info.ReleaseNotes = GetString(relData.Value, "body");
                    if (DateTime.TryParse(GetString(relData.Value, "published_at"), out var rel))
                        info.ReleasedAt = rel;
                }
            }
            catch { }

            return info;
        }

        private static async Task<JsonElement?> GetJsonAsync(string url)
        {
            using var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        private static string? GetString(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        private static string? GetNestedString(JsonElement e, string parent, string child)
        {
            if (e.TryGetProperty(parent, out var p) && p.ValueKind == JsonValueKind.Object
                && p.TryGetProperty(child, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
            return null;
        }

        private static int GetInt(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n))
                return n;
            return 0;
        }
    }
}
