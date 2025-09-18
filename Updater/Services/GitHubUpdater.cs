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
using System.Diagnostics;
using Updater.Models;
using Updater.Configuration;
using Updater.Utils;

namespace Updater
{
    public class GitHubUpdater
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/celarc/BMC/releases";
        private readonly string PersonalAccessToken = UpdaterConfig.Instance.GitHubPersonalAccessToken;

        private bool IsFileInUse(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            // First check if any BMC-related processes are running
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var processes = Process.GetProcessesByName(fileName);
            if (processes.Length == 0)
            {
                processes = Process.GetProcessesByName("BMC");
            }

            // If BMC processes are running, file is definitely in use
            if (processes.Length > 0)
            {
                foreach (var process in processes)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
                return true;
            }

            // Try multiple approaches to check if file can be written to
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // Try to open with exclusive access (no sharing)
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        return false; // File is available
                    }
                }
                catch (IOException)
                {
                    // File is locked, wait a bit and retry
                    if (i < 2)
                    {
                        System.Threading.Thread.Sleep(500);
                        continue;
                    }
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return true;
                }
            }

            return true; // Assume in use if we get here
        }

        private string GetFileUsageDiagnostic(string filePath)
        {
            if (!File.Exists(filePath))
                return "File does not exist";

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var processes = Process.GetProcessesByName(fileName);
            if (processes.Length == 0)
            {
                processes = Process.GetProcessesByName("BMC");
            }

            if (processes.Length > 0)
            {
                var processNames = string.Join(", ", processes.Select(p => $"{p.ProcessName} (PID: {p.Id})"));
                return $"File is in use by BMC processes: {processNames}";
            }

            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    return "File is available for writing";
                }
            }
            catch (IOException ex)
            {
                return $"File is locked by another process: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                return $"Access denied to file: {ex.Message}";
            }
        }

        private bool TryKillProcessesUsingFile(string filePath, IProgress<UpdateProgress> progress = null)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var processes = Process.GetProcessesByName(fileName);

                if (processes.Length == 0)
                {
                    processes = Process.GetProcessesByName("BMC");
                    if (processes.Length == 0)
                    {
                        processes = Process.GetProcessesByName("WebParam");
                    }
                }

                bool anyProcessKilled = false;
                foreach (var process in processes)
                {
                    try
                    {
                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.KillingProcess, process.ProcessName, process.Id)));
                        UpdaterLogger.LogInfo($"Killing process: {process.ProcessName} (PID: {process.Id})");

                        if (!process.HasExited)
                        {
                            process.Kill();
                            anyProcessKilled = true;

                            // Wait longer for process to fully exit
                            bool exited = process.WaitForExit(10000); // Wait up to 10 seconds
                            if (!exited)
                            {
                                progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ProcessDidNotExit, process.ProcessName)));
                                UpdaterLogger.LogWarning($"Process {process.ProcessName} did not exit within timeout");
                                continue;
                            }
                        }

                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ProcessTerminatedSuccessfully, process.ProcessName)));
                        UpdaterLogger.LogInfo($"Process {process.ProcessName} terminated successfully");
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.FailedToKillProcess, process.ProcessName, ex.Message)));
                        UpdaterLogger.LogError($"Failed to kill process {process.ProcessName}", ex);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch { }
                    }
                }

                if (anyProcessKilled)
                {
                    // Wait longer for file handles to be released after killing processes
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.WaitingForFileHandles));
                    UpdaterLogger.LogInfo("Waiting for file handles to be released");
                    System.Threading.Thread.Sleep(3000);
                }

                // Force garbage collection to help release any remaining handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Check if file is still in use after killing processes
                var stillInUse = IsFileInUse(filePath);
                if (stillInUse)
                {
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FileStillInUse));
                    UpdaterLogger.LogWarning("File is still in use after process termination");
                }
                else
                {
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FileNowAvailable));
                    UpdaterLogger.LogInfo("File is now available for replacement");
                }

                return !stillInUse;
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ErrorKillingProcesses, ex.Message)));
                UpdaterLogger.LogError("Error while trying to kill processes", ex);
                return false;
            }
        }

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

                progress?.Report(UpdateProgress.Create(0, SlovenianMessages.StartingGitHubDownload));
                UpdaterLogger.LogInfo("Starting GitHub download");

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
                            string.Format(SlovenianMessages.Downloading, fileName), fileName));

                        // Check if file is in use before downloading
                        if (IsFileInUse(targetFile))
                        {
                            var diagnostic = GetFileUsageDiagnostic(targetFile);
                            progress?.Report(UpdateProgress.Create(
                                (downloadedAssets * 100) / totalAssets,
                                string.Format(SlovenianMessages.FileInUse, fileName, diagnostic)));

                            if (!TryKillProcessesUsingFile(targetFile, progress))
                            {
                                // Last resort: try to rename the existing file and then download
                                progress?.Report(UpdateProgress.Create(
                                    (downloadedAssets * 100) / totalAssets,
                                    string.Format(SlovenianMessages.AttemptingToRename, fileName)));

                                try
                                {
                                    var backupFile = targetFile + ".backup." + DateTime.Now.Ticks;
                                    File.Move(targetFile, backupFile);
                                    progress?.Report(UpdateProgress.Create(
                                        (downloadedAssets * 100) / totalAssets,
                                        string.Format(SlovenianMessages.ExistingFileRenamed, Path.GetFileName(backupFile))));
                                }
                                catch (Exception ex)
                                {
                                    var finalDiagnostic = GetFileUsageDiagnostic(targetFile);
                                    var errorMsg = string.Format(SlovenianMessages.CouldNotDownload, fileName, finalDiagnostic, ex.Message);
                                    UpdaterLogger.LogError(errorMsg);
                                    return UpdateResult.CreateFailure(errorMsg);
                                }
                            }
                            else
                            {
                                progress?.Report(UpdateProgress.Create(
                                    (downloadedAssets * 100) / totalAssets,
                                    SlovenianMessages.ProcessesKilledSuccessfully));
                            }
                        }

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

                            // Add progress reporting for individual file download
                            var lastProgressTime = DateTime.Now;
                            var lastBytesReceived = 0L;

                            webClient.DownloadProgressChanged += (sender, e) =>
                            {
                                var currentTime = DateTime.Now;
                                var timeDiff = currentTime - lastProgressTime;

                                // Update progress every 250ms to avoid spam
                                if (timeDiff.TotalMilliseconds > 250)
                                {
                                    var bytesDiff = e.BytesReceived - lastBytesReceived;
                                    var speed = timeDiff.TotalSeconds > 0 ? bytesDiff / timeDiff.TotalSeconds : 0;

                                    // Calculate progress: base from completed assets + current asset progress
                                    var baseProgress = totalAssets > 0 ? (downloadedAssets * 100) / totalAssets : 0;
                                    var currentAssetProgress = e.TotalBytesToReceive > 0 ?
                                        (int)((e.BytesReceived * 100) / e.TotalBytesToReceive) : 0;
                                    var assetProgressContribution = totalAssets > 0 ? currentAssetProgress / totalAssets : currentAssetProgress;
                                    var overallProgress = Math.Min(100, baseProgress + assetProgressContribution);

                                    progress?.Report(UpdateProgress.CreateWithDetails(
                                        overallProgress,
                                        $"Prenašam: {fileName} ({(double)e.BytesReceived / e.TotalBytesToReceive:P0})",
                                        fileName,
                                        e.BytesReceived,
                                        e.TotalBytesToReceive,
                                        speed,
                                        "Prenašam"
                                    ));

                                    lastProgressTime = currentTime;
                                    lastBytesReceived = e.BytesReceived;
                                }
                            };

                            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                            await webClient.DownloadFileTaskAsync(downloadUrl, targetFile);
                        }

                        downloadedAssets++;
                        var percent = (downloadedAssets * 100) / totalAssets;
                        progress?.Report(UpdateProgress.Create(percent,
                            string.Format(SlovenianMessages.Downloaded, fileName), fileName));
                        UpdaterLogger.LogInfo($"Downloaded: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        // Stop downloading other files if any download fails
                        var errorMsg = string.Format(SlovenianMessages.DownloadFailed, asset.Name, ex.Message);
                        UpdaterLogger.LogError(errorMsg, ex);
                        return UpdateResult.CreateFailure(errorMsg, ex);
                    }
                }

                // Force file system sync to ensure all files are fully written
                progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FinalizingTransfer));
                UpdaterLogger.LogInfo("Forcing file system sync to ensure all GitHub files are written");

                // Force garbage collection to release any file handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Extended delay for GitHub downloads to ensure file system stability
                // GitHub downloads may need more time than FTP for files to be fully accessible
                progress?.Report(UpdateProgress.Create(100, "Finalizing file system operations..."));
                UpdaterLogger.LogInfo("Waiting for GitHub downloaded files to be fully accessible");
                System.Threading.Thread.Sleep(3000); // Increased from 1000ms to 3000ms

                // Additional verification that key files are accessible
                var mainExecutable = Path.Combine(targetPath, "BMC.exe");
                if (File.Exists(mainExecutable))
                {
                    // Try to ensure the main executable is fully written and accessible
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            using (var stream = File.Open(mainExecutable, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                if (stream.Length > 0)
                                {
                                    UpdaterLogger.LogInfo("BMC.exe is accessible and ready for version detection");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdaterLogger.LogWarning($"BMC.exe not yet accessible (attempt {i + 1}/5): {ex.Message}");
                            if (i < 4) System.Threading.Thread.Sleep(1000);
                        }
                    }
                }

                var successMsg = string.Format(SlovenianMessages.DownloadedFiles, downloadedAssets, totalAssets);
                UpdaterLogger.LogInfo(successMsg);
                return UpdateResult.CreateSuccess(successMsg, downloadedAssets);
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.CreateError(ex));
                var errorMsg = string.Format(SlovenianMessages.GitHubDownloadFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
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