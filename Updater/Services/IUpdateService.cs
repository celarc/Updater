using System;
using System.Threading.Tasks;
using Updater.Models;

namespace Updater.Services
{
    public interface IUpdateService
    {
        Task<UpdateResult> UpdateAsync(UpdateSource source, string targetPath,
            IProgress<UpdateProgress> progress = null);
        Task<VersionInfo> GetCurrentVersionAsync(string applicationPath);
        bool IsApplicationRunning(string processName);
        void StopRunningApplications(string processName);
    }

    public enum UpdateSource
    {
        FtpStable,
        FtpBeta,
        FtpWebParam,
        GitHub,
        Unknown
    }
}