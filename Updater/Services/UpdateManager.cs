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
                if (ProcessManager.IsApplicationRunningFromPath("BMC", _config.BMCPath))
                {
                    ProcessManager.StopApplicationsFromPath("BMC", _config.BMCPath);

                    // Verify that BMC process actually stopped from target path
                    if (ProcessManager.IsApplicationRunningFromPath("BMC", _config.BMCPath))
                    {
                        UpdaterLogger.LogError("BMC application could not be stopped for update");
                        return UpdateResult.CreateFailure(SlovenianMessages.BMCStillRunning);
                    }
                }

                if (source == UpdateSource.GitHub && gitHubRelease != null)
                {
                    var updater = new GitHubUpdater();
                    var result = await updater.UpdateFromGitHubAsync(gitHubRelease, _config.BMCPath, progress);

                    if (result.Success)
                    {
                        UpdaterLogger.LogInfo("GitHub update completed, allowing additional time for file system stabilization");
                        await Task.Delay(2000);
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
            return await VersionDetector.GetCurrentVersionAsync(_config.BMCPath);
        }

        public bool IsApplicationRunning(string processName)
        {
            return ProcessManager.IsApplicationRunning(processName);
        }

        public void StopRunningApplications(string processName)
        {
            ProcessManager.StopRunningApplications(processName);
        }

        public async Task WriteUpdateLogAsync(string logContent, string logType = "BMC")
        {
            var targetPath = logType == "BMC" ? _config.BMCPath : _config.WebParamPath;
            await UpdaterLogger.WriteUpdateLogAsync(logContent, targetPath);
        }

    }
}