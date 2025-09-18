using System;
using System.ComponentModel;
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

                    // First, get accurate total file count recursively
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.CountingFiles));
                    var totalFiles = CountFilesRecursively(session, remotePath, localPath);
                    var filesProcessed = 0;

                    UpdaterLogger.LogInfo($"Starting FTP download from {remotePath} to {localPath}");
                    UpdaterLogger.LogInfo($"Total files to download/update: {totalFiles}");

                    var directoryInfo = session.ListDirectory(remotePath);
                    var result = ProcessDirectory(directoryInfo, session, localPath, remotePath,
                        progress, ref filesProcessed, totalFiles);

                    // Force file system sync to ensure all files are fully written
                    progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FinalizingTransfer));
                    UpdaterLogger.LogInfo("Forcing file system sync to ensure all files are written");

                    // Force garbage collection to release any file handles
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    // Small additional delay to ensure file system operations are complete
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
                // Skip "." and ".." directories
                if (fileInfo.Name == "." || fileInfo.Name == "..")
                    continue;

                if (fileInfo.IsDirectory && !string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    var dirName = fileInfo.Name.ToLowerInvariant();
                    // Skip build artifacts and temporary directories
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
                    // Report starting download with current progress
                    var startPercent = totalFiles > 0 ? (int)((double)filesProcessed / totalFiles * 100) : 0;
                    progress?.Report(UpdateProgress.Create(startPercent,
                        string.Format(SlovenianMessages.Downloading, fileInfo.Name), fileInfo.Name));

                    // Download with detailed progress (filesProcessed is the current index)
                    updatedFiles += DownloadFile(session, fileInfo, remotePath, localPath, progress, filesProcessed, totalFiles);

                    // Increment after successful download
                    filesProcessed++;

                    // Report completion with updated progress
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

                // Check if file is in use and handle it
                if (File.Exists(targetFile) && IsFileInUse(targetFile))
                {
                    // Try to rename the locked file as backup
                    try
                    {
                        var backupFile = targetFile + ".backup." + DateTime.Now.Ticks;
                        File.Move(targetFile, backupFile);
                        UpdaterLogger.LogInfo($"Renamed locked file: {fileInfo.Name} to backup");
                    }
                    catch (Exception)
                    {
                        // If renaming fails, skip this file
                        UpdaterLogger.LogWarning($"Could not rename locked file: {fileInfo.Name}");
                        return 0;
                    }
                }

                // Set up transfer options
                var transferOptions = new TransferOptions
                {
                    TransferMode = TransferMode.Binary,
                    OverwriteMode = OverwriteMode.Overwrite
                };

                // For now, let's simplify and not use the FileTransferProgress event
                // as it's causing session conflicts. Report progress before and after.
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
            if (!File.Exists(filePath))
                return false;

            // First check if any BMC-related processes are running
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var processes = System.Diagnostics.Process.GetProcessesByName(fileName);
            if (processes.Length == 0)
            {
                processes = System.Diagnostics.Process.GetProcessesByName("BMC");
            }

            // If BMC processes are running, file is definitely in use
            if (processes.Length > 0)
            {
                foreach (var process in processes)
                {
                    try
                    {
                        process.Dispose();
                    }
                    catch { }
                }
                return true;
            }

            // Try multiple approaches to check if file can be written to
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    // Try to open with exclusive access (no sharing)
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        return false; // File is available
                    }
                }
                catch (IOException)
                {
                    // File is locked, wait a bit and retry
                    if (i < 2)
                    {
                        System.Threading.Thread.Sleep(500);
                        continue;
                    }
                    return true;
                }
                catch (UnauthorizedAccessException)
                {
                    return true;
                }
            }

            return true; // Assume in use if we get here
        }

        private int CountFiles(RemoteDirectoryInfo directoryInfo)
        {
            int count = 0;
            foreach (RemoteFileInfo fileInfo in directoryInfo.Files)
            {
                if (fileInfo.IsDirectory && !string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    // For directories, we need to recursively count files, but we can't do that here
                    // without another session call. We'll count directories as 1 for now
                    // and improve this in the ProcessDirectory method
                    count += 1;
                }
                else if (!string.IsNullOrEmpty(fileInfo.Name.Replace(".", "")))
                {
                    // Only count files that would actually be downloaded
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
                    // Skip "." and ".." directories
                    if (fileInfo.Name == "." || fileInfo.Name == "..")
                        continue;

                    // Skip directories we don't want to download
                    if (fileInfo.IsDirectory)
                    {
                        var dirName = fileInfo.Name.ToLowerInvariant();
                        // Skip build artifacts and temporary directories
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
                        // Only count files that would actually be downloaded (matching ShouldUpdateFile logic)
                        if (extension != ".fdb")
                        {
                            // If localPath is provided, check if file needs updating
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
                                // If no localPath, count all non-.fdb files
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
            return await Task.Run(() =>
            {
                try
                {
                    var exePath = Path.Combine(applicationPath, "BMC.exe");

                    // Check if file exists and is accessible
                    if (!File.Exists(exePath))
                    {
                        UpdaterLogger.LogWarning($"BMC.exe not found at: {exePath}");
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    // Verify file is not locked and can be accessed
                    if (!IsFileAccessible(exePath))
                    {
                        UpdaterLogger.LogWarning($"BMC.exe is locked or not accessible: {exePath}");
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    // Get basic file version info first (this is most reliable)
                    System.Diagnostics.FileVersionInfo versionInfo;
                    try
                    {
                        versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                        UpdaterLogger.LogInfo($"File version info obtained for: {exePath}");
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogError($"Failed to get FileVersionInfo for {exePath}", ex);
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    var productVersion = versionInfo.ProductVersion ?? "";
                    var fileVersion = versionInfo.FileVersion ?? "";
                    var comments = versionInfo.Comments ?? "";

                    // Try assembly loading with retries for informational version
                    var assemblyResult = TryGetAssemblyVersionInfo(exePath, fileVersion);
                    if (assemblyResult != null && assemblyResult.Channel != "Unknown")
                    {
                        UpdaterLogger.LogInfo($"Version extracted from assembly: {assemblyResult.DisplayVersion}");
                        return assemblyResult;
                    }

                    // Check product version (fallback)
                    if (!string.IsNullOrEmpty(productVersion))
                    {
                        var parsed = VersionInfo.ParseFromExecutable(productVersion);
                        if (parsed.Channel != "Unknown")
                        {
                            UpdaterLogger.LogInfo($"Version extracted from ProductVersion: {parsed.DisplayVersion}");
                            return parsed;
                        }
                    }

                    // Check comments field for channel info
                    if (!string.IsNullOrEmpty(comments))
                    {
                        var parsed = VersionInfo.ParseFromExecutable(comments);
                        if (parsed.Channel != "Unknown")
                        {
                            UpdaterLogger.LogInfo($"Version extracted from Comments: {parsed.DisplayVersion}");
                            return parsed;
                        }
                    }

                    // Fallback to file version
                    if (!string.IsNullOrEmpty(fileVersion))
                    {
                        var parsed = VersionInfo.ParseFromExecutable(fileVersion);
                        if (parsed.Channel != "Unknown")
                        {
                            UpdaterLogger.LogInfo($"Version extracted from FileVersion: {parsed.DisplayVersion}");
                            return parsed;
                        }
                    }

                    // Last resort: use file version with unknown channel
                    var fallbackResult = new VersionInfo
                    {
                        Version = fileVersion ?? "Unknown",
                        Channel = "Unknown"
                    };
                    UpdaterLogger.LogInfo($"Using fallback version: {fallbackResult.DisplayVersion}");
                    return fallbackResult;
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogError("Failed to get current version", ex);
                    return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                }
            });
        }

        private bool IsFileAccessible(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return stream.Length > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private VersionInfo TryGetAssemblyVersionInfo(string exePath, string fallbackVersion)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    UpdaterLogger.LogInfo($"Attempting to load assembly (attempt {attempt}/{maxRetries}): {exePath}");

                    var assembly = System.Reflection.Assembly.LoadFile(exePath);

                    // Check InformationalVersion attribute
                    var informationalVersionAttr = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                        .Cast<System.Reflection.AssemblyInformationalVersionAttribute>()
                        .FirstOrDefault();

                    if (informationalVersionAttr != null)
                    {
                        var informationalVersion = informationalVersionAttr.InformationalVersion ?? "";
                        if (!string.IsNullOrEmpty(informationalVersion))
                        {
                            var parsed = VersionInfo.ParseFromExecutable(informationalVersion);
                            if (parsed.Channel != "Unknown")
                            {
                                UpdaterLogger.LogInfo($"Found version in InformationalVersion: {parsed.DisplayVersion}");
                                return parsed;
                            }
                        }
                    }

                    // Check AssemblyConfiguration attribute
                    var configurationAttr = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyConfigurationAttribute), false)
                        .Cast<System.Reflection.AssemblyConfigurationAttribute>()
                        .FirstOrDefault();

                    if (configurationAttr != null)
                    {
                        var configuration = configurationAttr.Configuration ?? "";
                        if (configuration == "BETA" || configuration == "STABLE")
                        {
                            var result = new VersionInfo
                            {
                                Channel = configuration,
                                Version = fallbackVersion ?? "Unknown"
                            };
                            UpdaterLogger.LogInfo($"Found version in AssemblyConfiguration: {result.DisplayVersion}");
                            return result;
                        }
                    }

                    // Assembly loaded successfully but no version info found
                    UpdaterLogger.LogInfo("Assembly loaded but no version information found in attributes");
                    return null;
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Assembly load attempt {attempt} failed: {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        // Wait before retry
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            UpdaterLogger.LogWarning($"Failed to load assembly after {maxRetries} attempts");
            return null;
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
                    process.WaitForExit(5000); // Wait up to 5 seconds for process to exit
                }
                catch
                {
                    // Ignore errors when killing processes
                }
            }
        }
    }
}