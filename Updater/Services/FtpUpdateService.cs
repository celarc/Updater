using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinSCP;
using Updater.Configuration;
using Updater.Models;

namespace Updater.Services
{
    public class FtpUpdateService : IUpdateService
    {
        private readonly UpdaterConfig _config;

        public FtpUpdateService()
        {
            _config = UpdaterConfig.Instance;
        }

        public async Task<UpdateResult> UpdateAsync(UpdateSource source, string targetPath,
            IProgress<UpdateProgress> progress = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var remotePath = GetRemotePath(source);
                    return DownloadFromFtp(remotePath, targetPath, progress);
                }
                catch (Exception ex)
                {
                    return UpdateResult.CreateFailure($"FTP update failed: {ex.Message}", ex);
                }
            });
        }

        private string GetRemotePath(UpdateSource source)
        {
            switch (source)
            {
                case UpdateSource.FtpStable:
                    return "/STABLE/";
                case UpdateSource.FtpBeta:
                    return "/BETA/";
                case UpdateSource.FtpWebParam:
                    return "/6/";
                default:
                    return "/1/";
            }
        }

        private UpdateResult DownloadFromFtp(string remotePath, string localPath,
            IProgress<UpdateProgress> progress)
        {
            try
            {
                var sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = _config.FtpUrl,
                    UserName = _config.FtpUsername,
                    Password = _config.FtpPassword,
                };

                using (var session = new Session())
                {
                    session.Open(sessionOptions);

                    var directoryInfo = session.ListDirectory(remotePath);
                    var totalFiles = CountFiles(directoryInfo);
                    var filesProcessed = 0;

                    var result = ProcessDirectory(directoryInfo, session, localPath, remotePath,
                        progress, ref filesProcessed, totalFiles);

                    return UpdateResult.CreateSuccess($"Downloaded {result} files", result);
                }
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.CreateError(ex));
                return UpdateResult.CreateFailure($"FTP download failed: {ex.Message}", ex);
            }
        }

        private int ProcessDirectory(RemoteDirectoryInfo directoryInfo, Session session,
            string localPath, string remotePath, IProgress<UpdateProgress> progress,
            ref int filesProcessed, int totalFiles)
        {
            int updatedFiles = 0;

            foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
            {
                if (fileInfo.IsDirectory && !string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    var subDirLocal = Path.Combine(localPath, fileInfo.Name);
                    var subDirRemote = remotePath + fileInfo.Name + "/";

                    Directory.CreateDirectory(subDirLocal);

                    var subDirectoryInfo = session.ListDirectory(subDirRemote);
                    updatedFiles += ProcessDirectory(subDirectoryInfo, session, subDirLocal,
                        subDirRemote, progress, ref filesProcessed, totalFiles);
                }
                else if (ShouldUpdateFile(fileInfo, localPath))
                {
                    updatedFiles += DownloadFile(session, fileInfo, remotePath, localPath);

                    filesProcessed++;
                    var progressPercent = (int)((double)filesProcessed / totalFiles * 100);
                    progress?.Report(UpdateProgress.Create(progressPercent,
                        $"Downloaded: {fileInfo.Name}", fileInfo.Name));
                }
            }

            return updatedFiles;
        }

        private bool ShouldUpdateFile(RemoteFileInfo fileInfo, string localPath)
        {
            if (string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                return false;

            var extension = Path.GetExtension(fileInfo.Name).ToLowerInvariant();
            if (extension == ".fdb")
                return false;

            var localFilePath = Path.Combine(localPath, fileInfo.Name);
            return !File.Exists(localFilePath) ||
                   File.GetLastWriteTime(localFilePath) != fileInfo.LastWriteTime;
        }

        private int DownloadFile(Session session, RemoteFileInfo fileInfo,
            string remotePath, string localPath)
        {
            try
            {
                var sourceFile = RemotePath.EscapeFileMask(remotePath + fileInfo.Name);
                var transferResult = session.GetFiles(sourceFile, localPath);
                transferResult.Check();
                return transferResult.Transfers.Count;
            }
            catch
            {
                return 0;
            }
        }

        private int CountFiles(RemoteDirectoryInfo directoryInfo)
        {
            int count = 0;
            foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
            {
                if (fileInfo.IsDirectory && !string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    count += 1;
                }
                else if (!string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    count++;
                }
            }
            return count;
        }

        public async Task<VersionInfo> GetCurrentVersionAsync(string applicationPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var versionFilePath = Path.Combine(applicationPath, "version.txt");

                    if (File.Exists(versionFilePath))
                    {
                        var versionContent = File.ReadAllText(versionFilePath).Trim();
                        return VersionInfo.Parse(versionContent);
                    }

                    var exePath = Path.Combine(applicationPath, "BMC.exe");
                    if (File.Exists(exePath))
                    {
                        var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                        return new VersionInfo { Version = versionInfo.FileVersion, Channel = "Unknown" };
                    }

                    return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                }
                catch
                {
                    return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                }
            });
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