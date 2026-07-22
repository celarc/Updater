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

    /// <summary>
    /// Vir posodobitve. Ftp* = stari FTP prenos (WinSCP, primerjava po času datoteke),
    /// GitHub = prenos izbrane starejše verzije iz GitHub releases,
    /// Http* = novi prenos prek update.php s SHA256 primerjavo (s samodejnim FTP fallbackom).
    /// </summary>
    public enum UpdateSource
    {
        FtpStable,
        FtpBeta,
        FtpWebParam,
        GitHub,
        HttpStable,
        HttpBeta,
        HttpWebParam,
        Unknown
    }
}