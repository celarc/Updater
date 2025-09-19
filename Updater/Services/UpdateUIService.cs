using System;
using System.Linq;
using System.Threading.Tasks;
using Updater.Models;
using Updater.Utils;

namespace Updater.Services
{
    public class UpdateUIService
    {
        private readonly UpdateManager _updateManager;
        private readonly GitHubUpdater _gitHubUpdater;

        public UpdateUIService()
        {
            _updateManager = new UpdateManager();
            _gitHubUpdater = new GitHubUpdater();
        }

        public async Task<VersionInfo> LoadCurrentVersionAsync()
        {
            const int maxRetries = 3;
            int baseDelay = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await Task.Delay(baseDelay * attempt);
                    UpdaterLogger.LogInfo($"Attempting to load current version (attempt {attempt}/{maxRetries})");

                    var version = await _updateManager.GetCurrentBMCVersionAsync();

                    if (version != null && !string.IsNullOrEmpty(version.Version) && version.Version != "Unknown")
                    {
                        UpdaterLogger.LogInfo($"Current version loaded successfully: {version.DisplayVersion}");
                        return version;
                    }
                    else
                    {
                        UpdaterLogger.LogWarning($"Version load attempt {attempt} returned unknown/empty version");
                    }
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Version load attempt {attempt} failed: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        UpdaterLogger.LogError($"Could not load current version after {maxRetries} attempts", ex);
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    await Task.Delay(1000);
                }
            }

            return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
        }

        public async Task<GitHubRelease> GetLatestGitHubVersionAsync(bool isBeta)
        {
            try
            {
                var allReleases = await _gitHubUpdater.GetReleases();
                var filteredReleases = isBeta
                    ? allReleases.Where(r => r.Verzija.Contains("BETA")).ToList()
                    : allReleases.Where(r => r.Verzija.Contains("STABLE")).ToList();

                return filteredReleases.FirstOrDefault();
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Error loading GitHub versions for {(isBeta ? "beta" : "stable")}", ex);
                return null;
            }
        }

        public bool IsApplicationRunningCheck(string processName)
        {
            return _updateManager.IsApplicationRunning(processName);
        }

        public async Task<UpdateResult> PerformUpdateAsync(UpdateType updateType, UpdateSource source,
            IProgress<UpdateProgress> progress, GitHubRelease selectedRelease = null)
        {
            try
            {
                if (updateType == UpdateType.WebParam)
                {
                    return await _updateManager.UpdateWebParamAsync(progress);
                }
                else
                {
                    return await _updateManager.UpdateBMCAsync(source, progress, selectedRelease);
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError("Update failed in PerformUpdateAsync", ex);
                throw;
            }
        }

        public UpdateSource DetermineUpdateSource(UpdateType updateType, GitHubRelease selectedBetaRelease, GitHubRelease selectedStableRelease)
        {
            if (updateType == UpdateType.BMCBeta && selectedBetaRelease == null)
            {
                return UpdateSource.FtpBeta;
            }
            else if (updateType == UpdateType.BMCStable && selectedStableRelease == null)
            {
                return UpdateSource.FtpStable;
            }
            else
            {
                return UpdateSource.GitHub;
            }
        }

        public async Task WriteUpdateLogAsync(string logContent, string logType)
        {
            await _updateManager.WriteUpdateLogAsync(logContent, logType);
        }
    }
}