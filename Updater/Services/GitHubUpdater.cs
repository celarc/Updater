using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using Updater.Models;

namespace Updater
{
    public class GitHubUpdater
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/celarc/BMC/releases";
        private readonly string PersonalAccessToken = ""; // Kopiraj PAT za delovanje  C:\Users\BMC008\Desktop\githubPAT.txt

        public async Task<UpdateResult> UpdateFromGitHubAsync(GitHubRelease release, string targetPath,
            IProgress<UpdateProgress> progress = null)
        {
            try
            {
                if (release?.Assets == null || !release.Assets.Any())
                {
                    return UpdateResult.CreateFailure("No assets found in the selected release");
                }

                var totalAssets = release.Assets.Count;
                var downloadedAssets = 0;

                progress?.Report(UpdateProgress.Create(0, "Starting GitHub download..."));

                // Ensure target directory exists
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                foreach (var asset in release.Assets)
                {
                    try
                    {
                        var fileName = asset.Name;
                        var targetFile = Path.Combine(targetPath, fileName);

                        progress?.Report(UpdateProgress.Create(
                            (downloadedAssets * 100) / totalAssets,
                            $"Downloading: {fileName}", fileName));

                        using (var webClient = new WebClient())
                        {
                            webClient.Headers.Add("User-Agent", "Updater");
                            webClient.Headers.Add("Accept", "application/octet-stream");

                            if (!string.IsNullOrEmpty(PersonalAccessToken))
                            {
                                webClient.Headers.Add("Authorization", $"Bearer {PersonalAccessToken}");
                            }

                            string downloadUrl;
                            if (!string.IsNullOrEmpty(PersonalAccessToken))
                            {
                                downloadUrl = $"https://api.github.com/repos/celarc/BMC/releases/assets/{asset.Id}";
                            }
                            else
                            {
                                downloadUrl = asset.BrowserDownloadUrl;
                            }

                            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                            await webClient.DownloadFileTaskAsync(downloadUrl, targetFile);
                        }

                        downloadedAssets++;
                        var percent = (downloadedAssets * 100) / totalAssets;
                        progress?.Report(UpdateProgress.Create(percent,
                            $"Downloaded: {fileName}", fileName));
                    }
                    catch (Exception ex)
                    {
                        // Log but continue with other assets
                        progress?.Report(UpdateProgress.Create(
                            (downloadedAssets * 100) / totalAssets,
                            $"Failed to download {asset.Name}: {ex.Message}"));
                    }
                }

                return UpdateResult.CreateSuccess($"Downloaded {downloadedAssets} of {totalAssets} assets", downloadedAssets);
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.CreateError(ex));
                return UpdateResult.CreateFailure($"GitHub download failed: {ex.Message}", ex);
            }
        }

        public async Task<List<GitHubRelease>> GetReleases()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Updater");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    if (!string.IsNullOrEmpty(PersonalAccessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(
                                "Bearer",
                                PersonalAccessToken
                            );
                    }

                    var response = await httpClient.GetAsync(GitHubApiUrl);

                    var content = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception("Repository not found or no releases exist. Please verify:");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"GitHub API error: {response.StatusCode}\n{content}");
                    }

                    var releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(content);

                    if (releases == null || releases.Count == 0)
                    {
                        throw new Exception("API returned empty releases list despite success status");
                    }

                    return releases;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string Verzija { get; set; }

        [JsonProperty("name")]
        public string Ime { get; set; }

        [JsonProperty("published_at")]
        public DateTime Objavljeno { get; set; }

        [JsonProperty("body")]
        public string Opomba { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();
    }

    public class GitHubAsset
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}