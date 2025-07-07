using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Updater
{
    public class GitHubUpdater
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/celarc/BMC/releases";
        private readonly string PersonalAccessToken = "";

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

        public async Task DisplayGitHubReleases()
        {
            try
            {
                Console.WriteLine("Fetching releases...");
                var releases = await GetReleases();

                Console.WriteLine("\nAvailable Releases:");
                Console.WriteLine("===================");
                foreach (var release in releases)
                {
                    Console.WriteLine($"• Version: {release.Verzija ?? "null"}");
                    Console.WriteLine($"  Name: {release.Ime ?? "null"}");
                    Console.WriteLine($"  Date: {release.Objavljeno:yyyy-MM-dd}");
                    Console.WriteLine($"  Pre-release: {(release.Alpha ? "Yes" : "No")}");
                    Console.WriteLine($"  Notes: {(string.IsNullOrEmpty(release.Opomba) ? "None" : release.Opomba)}");
                    Console.WriteLine("-------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine("\nTroubleshooting Tips:");
                Console.WriteLine("1. Verify the repository exists at: https://github.com/celarc/BMC");
                Console.WriteLine("2. Check you have releases at: https://github.com/celarc/BMC/releases");
                Console.WriteLine("3. Try accessing the API directly: https://api.github.com/repos/celarc/BMC/releases");
                Console.WriteLine("4. If repository is private, you need authentication");
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

        [JsonProperty("prerelease")]
        public bool Alpha { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; }
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
    }
}