using System;
using System.Linq;
using System.Threading.Tasks;
using Updater.Models;
using Updater.Utils;

namespace Updater.Services
{
    public class GitHubUpdateService : IUpdateService
    {
        public async Task<UpdateResult> UpdateAsync(UpdateSource source, string targetPath,
            IProgress<UpdateProgress> progress = null)
        {
            if (source != UpdateSource.GitHub)
            {
                return UpdateResult.CreateFailure("GitHubUpdateService only handles GitHub downloads");
            }

            return UpdateResult.CreateFailure("GitHub download requires a specific release object");
        }

        public async Task<VersionInfo> GetCurrentVersionAsync(string applicationPath)
        {
            var ftpService = new FtpUpdateService();
            return await ftpService.GetCurrentVersionAsync(applicationPath);
        }

        public bool IsApplicationRunning(string processName)
        {
            return System.Diagnostics.Process.GetProcessesByName(processName).Any();
        }

        public void StopRunningApplications(string processName)
        {
            var processes = System.Diagnostics.Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Failed to kill process {process.ProcessName}: {ex.Message}");
                }
            }
        }
    }
}