using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        // ===== 供 XAML 绑定的展示用属性（不参与缓存序列化）=====

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

        [JsonIgnore]
        public BitmapImage? OwnerAvatarImage
        {
            get
            {
                if (string.IsNullOrWhiteSpace(OwnerAvatarUrl)) return null;
                try { return new BitmapImage(new Uri(OwnerAvatarUrl)); }
                catch { return null; }
            }
        }

        [JsonIgnore]
        public SolidColorBrush LanguageColorBrush
        {
            get
            {
                string hex = LanguageColors.TryGetValue(Language ?? "", out var c) ? c : "#808080";
                return new SolidColorBrush(ParseHexColor(hex));
            }
        }

        [JsonIgnore]
        public string UpdatedText => UpdatedAt.HasValue ? UpdatedAt.Value.ToString("yyyy-MM-dd") : "";

        [JsonIgnore]
        public string ReleaseBadgeText =>
            string.IsNullOrWhiteSpace(LatestReleaseTag) ? "" : Loc.T("最新版本") + " " + LatestReleaseTag;

        [JsonIgnore]
        public string ReleaseDateText =>
            ReleasedAt.HasValue ? ReleasedAt.Value.ToString("yyyy-MM-dd") : "";

        [JsonIgnore]
        public Visibility ReleaseVisibility =>
            string.IsNullOrWhiteSpace(LatestReleaseTag) ? Visibility.Collapsed : Visibility.Visible;

        [JsonIgnore]
        public Visibility NotesVisibility =>
            string.IsNullOrWhiteSpace(CleanNotes) ? Visibility.Collapsed : Visibility.Visible;

        [JsonIgnore]
        public Visibility DescriptionVisibility =>
            string.IsNullOrWhiteSpace(Description) ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>去掉 Markdown 标记 / 隐藏注释后的发布说明摘要。</summary>
        [JsonIgnore]
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

    /// <summary>主页资讯拉取结果（含缓存 / 错误信息）。</summary>
    public class NewsResult
    {
        public List<RepoInfo> Repos { get; set; } = new();
        public bool FromCache { get; set; }
        public DateTime? FetchedAt { get; set; }
        public bool RateLimited { get; set; }
        public bool Failed { get; set; }
    }

    /// <summary>主页资讯源：从指定的 GitHub 仓库获取信息（带本地缓存，规避 API 限流）。</summary>
    public static class GitHubNewsService
    {
        private const string Owner = "MEIMAOA";
        private const int CacheMinutes = 30;

        private static readonly string[] RepoNames =
        {
            "ShutdownTimer-Edu",
            "ShutdownTimer",
            "SysKit",
            "SnakeLauncher",
            "SnakeGame"
        };

        private static readonly HttpClient _http = CreateHttpClient();

        private static string CachePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Chronos", "home_cache.json");

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ShutdownTimer/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");
            return client;
        }

        /// <summary>拉取资讯。forceRefresh=false 时优先使用 30 分钟内的缓存。</summary>
        public static async Task<NewsResult> FetchAsync(bool forceRefresh = false)
        {
            var result = new NewsResult();

            var cached = LoadCache();
            if (!forceRefresh && cached != null
                && (DateTime.Now - cached.Value.fetchedAt).TotalMinutes < CacheMinutes)
            {
                result.Repos = cached.Value.repos;
                result.FromCache = true;
                result.FetchedAt = cached.Value.fetchedAt;
                return result;
            }

            // 优先从国内镜像获取静态 news.json（只需 1 次请求，国内速度快）
            var mirrored = await TryFetchFromMirrorAsync();
            if (mirrored != null && mirrored.Count > 0)
            {
                SaveCache(mirrored);
                result.Repos = mirrored;
                result.FetchedAt = DateTime.Now;
                return result;
            }

            var repos = new List<RepoInfo>();
            bool rateLimited = false;

            foreach (var repo in RepoNames)
            {
                try
                {
                    var (info, status) = await FetchRepoAsync(repo);
                    if (info != null)
                    {
                        repos.Add(info);
                    }
                    else if (status == HttpStatusCode.Forbidden || status == HttpStatusCode.TooManyRequests)
                    {
                        rateLimited = true;
                    }
                }
                catch { }
            }

            if (repos.Count == 0)
            {
                // 拉取失败 → 退回到缓存（如果有）
                if (cached != null)
                {
                    result.Repos = cached.Value.repos;
                    result.FromCache = true;
                    result.FetchedAt = cached.Value.fetchedAt;
                    result.RateLimited = rateLimited;
                    return result;
                }

                result.Failed = true;
                result.RateLimited = rateLimited;
                return result;
            }

            SaveCache(repos);
            result.Repos = repos;
            result.FetchedAt = DateTime.Now;
            return result;
        }

        /// <summary>资讯镜像：官方仓库根目录的 news.json，经 GitHub 加速镜像获取。</summary>
        private static readonly string[] NewsMirrors =
        {
            "https://ghfast.top/https://github.com/MEIMAOA/ShutdownTimer/raw/main/news.json",
            "https://ghproxy.net/https://github.com/MEIMAOA/ShutdownTimer/raw/main/news.json",
            "https://github.com/MEIMAOA/ShutdownTimer/raw/main/news.json"
        };

        /// <summary>从镜像获取静态 news.json（一次请求拿全部仓库资讯），失败返回 null。</summary>
        private static async Task<List<RepoInfo>?> TryFetchFromMirrorAsync()
        {
            foreach (var url in NewsMirrors)
            {
                try
                {
                    using var response = await _http.GetAsync(url).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode) continue;

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    JsonElement arr;
                    if (root.ValueKind == JsonValueKind.Array) arr = root;
                    else if (root.TryGetProperty("repos", out var r) && r.ValueKind == JsonValueKind.Array) arr = r;
                    else continue;

                    var list = new List<RepoInfo>();
                    foreach (var item in arr.EnumerateArray())
                    {
                        var info = ParseRepoFromNews(item);
                        if (info != null) list.Add(info);
                    }
                    if (list.Count > 0) return list;
                }
                catch { }
            }
            return null;
        }

        private static RepoInfo? ParseRepoFromNews(JsonElement e)
        {
            try
            {
                var info = new RepoInfo
                {
                    FullName = GetString(e, "full_name") ?? "",
                    Name = GetString(e, "name") ?? "",
                    Description = GetString(e, "description") ?? "",
                    Stars = GetInt(e, "stars"),
                    Forks = GetInt(e, "forks"),
                    Language = GetString(e, "language") ?? "",
                    HtmlUrl = GetString(e, "html_url") ?? "",
                    OwnerAvatarUrl = GetString(e, "owner_avatar_url") ?? "",
                    LatestReleaseTag = GetString(e, "latest_release_tag"),
                    LatestReleaseName = GetString(e, "latest_release_name"),
                    ReleaseNotes = GetString(e, "release_notes")
                };

                if (DateTime.TryParse(GetString(e, "updated_at"), out var u)) info.UpdatedAt = u;
                if (DateTime.TryParse(GetString(e, "released_at"), out var r)) info.ReleasedAt = r;

                return (info.FullName.Length > 0 || info.Name.Length > 0) ? info : null;
            }
            catch { return null; }
        }

        private static async Task<(RepoInfo? info, HttpStatusCode status)> FetchRepoAsync(string repo)
        {
            var (repoData, status) = await GetJsonAsync($"https://api.github.com/repos/{Owner}/{repo}");
            if (repoData == null) return (null, status);

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

            // 最新 Release：失败不影响仓库信息（限流时通常会失败）
            try
            {
                var (relData, _) = await GetJsonAsync($"https://api.github.com/repos/{Owner}/{repo}/releases/latest");
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

            return (info, status);
        }

        /// <summary>国内 GitHub API 代理前缀（allorigins 可转发 api.github.com）。</summary>
        private const string ApiProxyPrefix = "https://api.allorigins.win/raw?url=";

        private static async Task<(JsonElement? data, HttpStatusCode status)> GetJsonAsync(string apiUrl)
        {
            // 国内优先走代理镜像，失败回退直连
            var urls = new[]
            {
                ApiProxyPrefix + Uri.EscapeDataString(apiUrl),
                apiUrl
            };

            HttpStatusCode lastStatus = HttpStatusCode.ServiceUnavailable;

            foreach (var url in urls)
            {
                try
                {
                    using var response = await _http.GetAsync(url).ConfigureAwait(false);
                    lastStatus = response.StatusCode;
                    if (!response.IsSuccessStatusCode) continue;

                    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // 只接受有效数据：仓库信息含 full_name，Release 信息含 tag_name
                    if (root.ValueKind == JsonValueKind.Object &&
                        (root.TryGetProperty("full_name", out _) || root.TryGetProperty("tag_name", out _)))
                    {
                        return (root.Clone(), response.StatusCode);
                    }
                }
                catch { }
            }

            return (null, lastStatus);
        }

        // ==================== 缓存 ====================

        private class CacheData
        {
            public DateTime FetchedAt { get; set; }
            public List<RepoInfo> Repos { get; set; } = new();
        }

        private static (List<RepoInfo> repos, DateTime fetchedAt)? LoadCache()
        {
            try
            {
                if (!File.Exists(CachePath)) return null;
                var data = JsonSerializer.Deserialize<CacheData>(File.ReadAllText(CachePath));
                if (data == null || data.Repos.Count == 0) return null;
                return (data.Repos, data.FetchedAt);
            }
            catch { return null; }
        }

        private static void SaveCache(List<RepoInfo> repos)
        {
            try
            {
                string? dir = Path.GetDirectoryName(CachePath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var data = new CacheData { FetchedAt = DateTime.Now, Repos = repos };
                File.WriteAllText(CachePath, JsonSerializer.Serialize(data));
            }
            catch { }
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
