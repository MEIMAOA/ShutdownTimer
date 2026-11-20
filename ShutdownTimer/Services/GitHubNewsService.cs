using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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
        public string? LatestReleaseTag { get; set; }
        public string? LatestReleaseName { get; set; }
        public string? ReleaseNotes { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
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
                HtmlUrl = GetString(repoData.Value, "html_url") ?? $"https://github.com/{Owner}/{repo}"
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

        private static int GetInt(JsonElement e, string name)
        {
            if (e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n))
                return n;
            return 0;
        }
    }
}
