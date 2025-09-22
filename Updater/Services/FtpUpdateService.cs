using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WinSCP;
using Updater.Configuration;
using Updater.Models;
using Updater.Utils;

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
                    var errorMsg = string.Format(SlovenianMessages.FtpDownloadFailed, ex.Message);
                    UpdaterLogger.LogError(errorMsg, ex);
                    return UpdateResult.CreateFailure(errorMsg, ex);
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

                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.CountingFiles));
                    var totalFiles = CountFilesRecursively(session, remotePath, localPath);
                    var filesProcessed = 0;

                    UpdaterLogger.LogInfo($"Starting FTP download from {remotePath} to {localPath}");
                    UpdaterLogger.LogInfo($"Total files to download/update: {totalFiles}");

                    var directoryInfo = session.ListDirectory(remotePath);
                    var result = ProcessDirectory(directoryInfo, session, localPath, remotePath,
                        progress, ref filesProcessed, totalFiles);

                    progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FinalizingTransfer));
                    UpdaterLogger.LogInfo("Forcing file system sync to ensure all files are written");

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    System.Threading.Thread.Sleep(1000);

                    var successMsg = string.Format(SlovenianMessages.DownloadedFilesCount, result);
                    UpdaterLogger.LogInfo(successMsg);
                    return UpdateResult.CreateSuccess(successMsg, result);
                }
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.CreateError(ex));
                var errorMsg = string.Format(SlovenianMessages.FtpDownloadFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        private int ProcessDirectory(RemoteDirectoryInfo directoryInfo, Session session,
            string localPath, string remotePath, IProgress<UpdateProgress> progress,
            ref int filesProcessed, int totalFiles)
        {
            int updatedFiles = 0;

            foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
            {
                if (fileInfo.Name == "." || fileInfo.Name == "..")
                    continue;

                if (fileInfo.IsDirectory && !string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    var dirName = fileInfo.Name.ToLowerInvariant();
                    if (dirName.StartsWith(".") || dirName == "app.publish" ||
                        dirName == "node_modules" || dirName == "obj" || dirName == "bin")
                    {
                        UpdaterLogger.LogInfo($"  Skipping directory in processing: {fileInfo.Name}");
                        continue;
                    }

                    var subDirLocal = Path.Combine(localPath, fileInfo.Name);
                    var subDirRemote = remotePath + fileInfo.Name + "/";

                    Directory.CreateDirectory(subDirLocal);

                    var subDirectoryInfo = session.ListDirectory(subDirRemote);
                    updatedFiles += ProcessDirectory(subDirectoryInfo, session, subDirLocal,
                        subDirRemote, progress, ref filesProcessed, totalFiles);
                }
                else if (ShouldUpdateFile(fileInfo, localPath))
                {
                    var startPercent = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 0;
                    progress?.Report(UpdateProgress.Create(startPercent,
                        string.Format(SlovenianMessages.Downloading, fileInfo.Name), fileInfo.Name));

                    updatedFiles += DownloadFile(session, fileInfo, remotePath, localPath, progress, filesProcessed, totalFiles);

                    filesProcessed++;

                    var endPercent = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 100;
                    progress?.Report(UpdateProgress.Create(endPercent,
                        string.Format(SlovenianMessages.Downloaded, fileInfo.Name), fileInfo.Name));
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
            string remotePath, string localPath, IProgress<UpdateProgress> progress = null,
            int currentFileIndex = 0, int totalFiles = 0)
        {
            try
            {
                var sourceFile = RemotePath.EscapeFileMask(remotePath + fileInfo.Name);
                var targetFile = Path.Combine(localPath, fileInfo.Name);

                if (File.Exists(targetFile) && IsFileInUse(targetFile))
                {
                    UpdaterLogger.LogInfo($"File {fileInfo.Name} is in use, attempting to free it");

                    // Try to kill processes using the file (same as GitHub download)
                    if (!TryKillProcessesUsingFile(targetFile))
                    {
                        // Fallback to backup file creation
                        try
                        {
                            FileOperations.CreateBackupFile(targetFile);
                            UpdaterLogger.LogInfo($"Created backup for locked file: {fileInfo.Name}");
                        }
                        catch (Exception ex)
                        {
                            UpdaterLogger.LogWarning($"Could not rename locked file: {fileInfo.Name} - {ex.Message}");
                            return 0;
                        }
                    }
                    else
                    {
                        UpdaterLogger.LogInfo($"Successfully freed file: {fileInfo.Name}");
                    }
                }

                var transferOptions = new TransferOptions
                {
                    TransferMode = TransferMode.Binary,
                    OverwriteMode = OverwriteMode.Overwrite
                };

                if (progress != null && totalFiles > 0)
                {
                    var startProgress = (currentFileIndex * 100) / totalFiles;
                    progress.Report(UpdateProgress.Create(startProgress,
                        $"Prenašam: {fileInfo.Name}", fileInfo.Name));
                }

                var transferResult = session.GetFiles(sourceFile, localPath, false, transferOptions);
                transferResult.Check();

                UpdaterLogger.LogInfo($"Successfully downloaded: {fileInfo.Name}");
                return transferResult.Transfers.Count;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Failed to download file: {fileInfo.Name}", ex);
                return 0;
            }
        }

        private bool IsFileInUse(string filePath)
        {
            return FileOperations.IsFileInUse(filePath);
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
                    var extension = Path.GetExtension(fileInfo.Name).ToLowerInvariant();
                    if (extension != ".fdb")
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        private int CountFilesRecursively(Session session, string remotePath, string localPath = null)
        {
            try
            {
                var directoryInfo = session.ListDirectory(remotePath);
                int count = 0;

                foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
                {
                    if (fileInfo.Name == "." || fileInfo.Name == "..")
                        continue;

                    if (fileInfo.IsDirectory)
                    {
                        var dirName = fileInfo.Name.ToLowerInvariant();
                        if (dirName.StartsWith(".") || dirName == "app.publish" ||
                            dirName == "node_modules" || dirName == "obj" || dirName == "bin")
                        {
                            UpdaterLogger.LogInfo($"  Skipping directory: {fileInfo.Name}");
                            continue;
                        }

                        if (!string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                        {
                            var subDirRemote = remotePath + fileInfo.Name + "/";
                            var subDirLocal = localPath != null ? Path.Combine(localPath, fileInfo.Name) : null;
                            var subCount = CountFilesRecursively(session, subDirRemote, subDirLocal);
                            count += subCount;

                            if (subCount > 0)
                            {
                                UpdaterLogger.LogInfo($"  Subdirectory {fileInfo.Name}: {subCount} files");
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                    {
                        var extension = Path.GetExtension(fileInfo.Name).ToLowerInvariant();
                        if (extension != ".fdb")
                        {
                            if (localPath != null)
                            {
                                var localFilePath = Path.Combine(localPath, fileInfo.Name);
                                if (!File.Exists(localFilePath) ||
                                    File.GetLastWriteTime(localFilePath) != fileInfo.LastWriteTime)
                                {
                                    count++;
                                }
                            }
                            else
                            {
                                count++;
                            }
                        }
                    }
                }

                if (count > 0 && remotePath.EndsWith("/"))
                {
                    UpdaterLogger.LogInfo($"Directory {remotePath}: {count} files total");
                }

                return count;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Error counting files in {remotePath}", ex);
                return 0;
            }
        }

        public async Task<VersionInfo> GetCurrentVersionAsync(string applicationPath)
        {
            return await VersionDetector.GetCurrentVersionAsync(applicationPath);
        }



        public bool IsApplicationRunning(string processName)
        {
            return ProcessManager.IsApplicationRunning(processName);
        }

        public void StopRunningApplications(string processName)
        {
            ProcessManager.StopRunningApplications(processName);
        }

        private bool TryKillProcessesUsingFile(string filePath)
        {
            try
            {
                UpdaterLogger.LogInfo($"Attempting to free file: {filePath}");

                // Get the target directory and file name
                var targetDirectory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                // Check if BMC processes are running from the target directory
                if (ProcessManager.IsApplicationRunningFromPath("BMC", targetDirectory))
                {
                    UpdaterLogger.LogInfo("Stopping BMC processes from target directory");
                    ProcessManager.StopApplicationsFromPath("BMC", targetDirectory);

                    // Wait for processes to fully terminate
                    System.Threading.Thread.Sleep(2000);

                    // Force garbage collection to help release file handles
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Check if file is now available
                    if (!FileOperations.IsFileInUse(filePath))
                    {
                        UpdaterLogger.LogInfo("File is now available after stopping processes");
                        return true;
                    }
                }

                // Check if specific file process is running
                if (ProcessManager.IsApplicationRunning(fileName))
                {
                    UpdaterLogger.LogInfo($"Stopping {fileName} processes");
                    ProcessManager.StopRunningApplications(fileName);

                    // Wait for processes to terminate
                    System.Threading.Thread.Sleep(2000);

                    // Check if file is now available
                    if (!FileOperations.IsFileInUse(filePath))
                    {
                        UpdaterLogger.LogInfo($"File is now available after stopping {fileName} processes");
                        return true;
                    }
                }

                UpdaterLogger.LogWarning("Could not free the file through process termination");
                return false;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Error while trying to kill processes using file: {ex.Message}", ex);
                return false;
            }
        }
    }
}