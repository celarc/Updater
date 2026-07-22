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
        private readonly IUpdateService _httpUpdateService;
        private readonly GitHubUpdateService _gitHubUpdateService;
        private readonly UpdaterConfig _config;

        public UpdateManager(IUpdateService ftpUpdateService = null)
        {
            _ftpUpdateService = ftpUpdateService ?? new FtpUpdateService();
            _httpUpdateService = new HttpUpdateService();
            _gitHubUpdateService = new GitHubUpdateService();
            _config = UpdaterConfig.Instance;
        }

        /// <summary>
        /// Posodobi BMC iz izbranega vira: GitHub (izbrana starejša verzija),
        /// HttpStable/HttpBeta (novi HTTP+SHA256 prenos s samodejnim FTP fallbackom)
        /// ali FtpStable/FtpBeta (stari FTP prenos - gumbi "Stari način (FTP)").
        /// Pred prenosom vedno ustavi BMC procese, ki tečejo iz ciljne mape.
        /// </summary>
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
                else if (source == UpdateSource.HttpStable || source == UpdateSource.HttpBeta)
                {
                    return await UpdateWithFtpFallbackAsync(source,
                        source == UpdateSource.HttpStable ? UpdateSource.FtpStable : UpdateSource.FtpBeta,
                        _config.BMCPath, progress);
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

        /// <summary>
        /// Posodobi WebParam. Privzeto poskusi novi HTTP+SHA256 prenos in ob napaki
        /// samodejno preklopi na FTP; s forceFtp=true (gumb "Stari način (FTP)")
        /// gre direktno na stari FTP prenos brez HTTP poskusa.
        /// </summary>
        public async Task<UpdateResult> UpdateWebParamAsync(IProgress<UpdateProgress> progress = null,
            bool forceFtp = false)
        {
            try
            {
                if (_ftpUpdateService.IsApplicationRunning("WebParam"))
                {
                    _ftpUpdateService.StopRunningApplications("WebParam");
                }

                if (forceFtp)
                {
                    return await _ftpUpdateService.UpdateAsync(UpdateSource.FtpWebParam,
                        _config.WebParamPath, progress);
                }

                return await UpdateWithFtpFallbackAsync(UpdateSource.HttpWebParam,
                    UpdateSource.FtpWebParam, _config.WebParamPath, progress);
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format(SlovenianMessages.WebParamUpdateFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        /// <summary>
        /// Jedro fallback logike: najprej poskusi HTTP+SHA256 prenos; če ta vrne napako
        /// (ni povezave, napačno geslo, pokvarjen prenos ...), o tem obvesti uporabnika
        /// in posodobitev dokonča prek starega FTP prenosa. Če je v BMC.ini nastavljen
        /// UPDATE_DISABLE_HTTP=1, se HTTP preskoči in gre direktno na FTP (varovalka).
        /// </summary>
        private async Task<UpdateResult> UpdateWithFtpFallbackAsync(UpdateSource httpSource,
            UpdateSource ftpSource, string targetPath, IProgress<UpdateProgress> progress)
        {
            if (!_config.DisableHttpUpdate)
            {
                var httpResult = await _httpUpdateService.UpdateAsync(httpSource, targetPath, progress);
                if (httpResult.Success)
                {
                    return httpResult;
                }

                UpdaterLogger.LogWarning($"HTTP update failed ({httpResult.Message}), falling back to FTP");
                progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FallingBackToFtp));
            }
            else
            {
                UpdaterLogger.LogInfo("HTTP update disabled via configuration, using FTP");
            }

            return await _ftpUpdateService.UpdateAsync(ftpSource, targetPath, progress);
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