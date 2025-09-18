using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Updater.Configuration;
using Updater.Models;

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
            // Use the same improved version detection as FTP service
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
                catch
                {
                    // Ignore errors when killing processes
                }
            }
        }
    }
}