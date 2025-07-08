// Update the UpdateManager to handle GitHub downloads
// Services/UpdateManager.cs - Updated
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Updater.Models;
using Updater.Configuration;

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
                }

                if (source == UpdateSource.GitHub && gitHubRelease != null)
                {
                    var updater = new GitHubUpdater(); //Legacy method for downloading github releases
                    return await updater.UpdateFromGitHubAsync(gitHubRelease, _config.BMCPath, progress);
                }
                else
                {
                    return await _ftpUpdateService.UpdateAsync(source, _config.BMCPath, progress);
                }
            }
            catch (Exception ex)
            {
                return UpdateResult.CreateFailure($"BMC update failed: {ex.Message}", ex);
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
                return UpdateResult.CreateFailure($"WebParam update failed: {ex.Message}", ex);
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
            await Task.Run(() =>
            {
                try
                {
                    var logPath = logType == "BMC" ? _config.BMCPath : _config.WebParamPath;
                    var logDir = Path.Combine(logPath, "Log");

                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    var logFile = Path.Combine(logDir, "AutoUpdate.txt");
                    File.AppendAllText(logFile, logContent);
                }
                catch
                {
                    // Ignore logging errors
                }
            });
        }
    }
}