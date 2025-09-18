// Update the UpdateManager to handle GitHub downloads
// Services/UpdateManager.cs - Updated
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Updater.Models;
using Updater.Configuration;
using Updater.Utils;

namespace Updater.Services
{
    public class UpdateManager
    {
        private readonly IUpdateService _ftpUpdateService;
        private readonly GitHubUpdateService _gitHubUpdateService;
        private readonly UpdaterConfig _config;

        public UpdateManager(IUpdateService ftpUpdateService = null)
        {
            _ftpUpdateService = ftpUpdateService ?? new FtpUpdateService();
            _gitHubUpdateService = new GitHubUpdateService();
            _config = UpdaterConfig.Instance;
        }

        public async Task<UpdateResult> UpdateBMCAsync(UpdateSource source,
            IProgress<UpdateProgress> progress = null, GitHubRelease gitHubRelease = null)
        {
            try
            {
                if (_ftpUpdateService.IsApplicationRunning("BMC"))
                {
                    _ftpUpdateService.StopRunningApplications("BMC");

                    // Verify that BMC process actually stopped
                    if (_ftpUpdateService.IsApplicationRunning("BMC"))
                    {
                        UpdaterLogger.LogError("BMC application could not be stopped for update");
                        return UpdateResult.CreateFailure(SlovenianMessages.BMCStillRunning);
                    }
                }

                if (source == UpdateSource.GitHub && gitHubRelease != null)
                {
                    var updater = new GitHubUpdater(); //Legacy method for downloading github releases
                    var result = await updater.UpdateFromGitHubAsync(gitHubRelease, _config.BMCPath, progress);

                    // Additional delay after GitHub updates to ensure files are fully accessible
                    if (result.Success)
                    {
                        UpdaterLogger.LogInfo("GitHub update completed, allowing additional time for file system stabilization");
                        await Task.Delay(2000); // Additional 2-second delay for GitHub updates
                    }

                    return result;
                }
                else
                {
                    return await _ftpUpdateService.UpdateAsync(source, _config.BMCPath, progress);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format(SlovenianMessages.BMCUpdateFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        public async Task<UpdateResult> UpdateWebParamAsync(IProgress<UpdateProgress> progress = null)
        {
            try
            {
                if (_ftpUpdateService.IsApplicationRunning("WebParam"))
                {
                    _ftpUpdateService.StopRunningApplications("WebParam");
                }

                return await _ftpUpdateService.UpdateAsync(UpdateSource.FtpWebParam,
                    _config.WebParamPath, progress);
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format(SlovenianMessages.WebParamUpdateFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        public async Task<VersionInfo> GetCurrentBMCVersionAsync()
        {
            return await _ftpUpdateService.GetCurrentVersionAsync(_config.BMCPath);
        }

        public bool IsApplicationRunning(string processName)
        {
            return _ftpUpdateService.IsApplicationRunning(processName);
        }

        public void StopRunningApplications(string processName)
        {
            _ftpUpdateService.StopRunningApplications(processName);
        }

        public async Task WriteUpdateLogAsync(string logContent, string logType = "BMC")
        {
            var targetPath = logType == "BMC" ? _config.BMCPath : _config.WebParamPath;
            await UpdaterLogger.WriteUpdateLogAsync(logContent, targetPath);
        }
    }
}