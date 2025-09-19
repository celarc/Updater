using System;
using System.IO;
using Updater.Configuration;

namespace Updater.Utils
{
    public static class NetworkFileHandler
    {
        public static bool IsNetworkPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            // Check for UNC paths (\\server\share)
            if (path.StartsWith(@"\\"))
                return true;

            // Check for mapped network drives by testing if it's a network drive
            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(path));
                return driveInfo.DriveType == DriveType.Network;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNetworkFileInUse(string filePath)
        {
            UpdaterLogger.LogInfo($"Checking network file usage for: {filePath}");

            var isNetworkPath = IsNetworkPath(filePath);
            if (!isNetworkPath)
            {
                return FileOperations.IsFileInUse(filePath);
            }

            UpdaterLogger.LogInfo("Using network detection strategies");

            // For network paths, use conservative detection
            try
            {
                if (CheckForRemoteProcesses(filePath))
                {
                    UpdaterLogger.LogWarning("Remote BMC processes detected - FILE IS IN USE");
                    return true;
                }

                if (IsFileLockedOnNetwork(filePath))
                {
                    UpdaterLogger.LogWarning("Network file is locked - FILE IS IN USE");
                    return true;
                }

                // Conservative fallback for network files
                UpdaterLogger.LogWarning("CONSERVATIVE MODE: Assuming network file might be in use");
                return true;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Network file detection failed: {ex.Message}");
                return true; // Assume in use for safety
            }
        }

        private static bool CheckForRemoteProcesses(string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);

                // Check for lock files created by BMC instances
                return BMCLockFileManager.CheckForActiveBMCLocks(directory);
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not check for remote processes: {ex.Message}");
                return false;
            }
        }

        private static bool IsFileLockedOnNetwork(string filePath)
        {
            UpdaterLogger.LogInfo($"Testing file lock on network path: {filePath}");

            var maxAttempts = 8;
            var delayBetweenAttempts = 400;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    UpdaterLogger.LogInfo($"Network file lock test attempt {attempt}/{maxAttempts}");

                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        var originalLength = stream.Length;
                        stream.Seek(0, SeekOrigin.End);
                        stream.WriteByte(0);
                        stream.SetLength(originalLength);
                        stream.Flush();

                        UpdaterLogger.LogInfo("Network file is available for exclusive write access");
                        return false;
                    }
                }
                catch (IOException ex)
                {
                    UpdaterLogger.LogInfo($"Network file appears locked (attempt {attempt}): {ex.Message}");

                    if (TryAlternativeFileAccessTests(filePath, attempt))
                    {
                        return false;
                    }

                    if (attempt < maxAttempts)
                    {
                        var progressiveDelay = delayBetweenAttempts + (attempt * 100);
                        UpdaterLogger.LogInfo($"Waiting {progressiveDelay}ms before retry...");
                        System.Threading.Thread.Sleep(progressiveDelay);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    UpdaterLogger.LogWarning($"Access denied to network file: {ex.Message}");
                    return true;
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Unexpected error testing network file lock: {ex.Message}");
                    return true;
                }
            }

            UpdaterLogger.LogWarning($"Network file appears locked after {maxAttempts} attempts");
            return true;
        }

        private static bool TryAlternativeFileAccessTests(string filePath, int attempt)
        {
            try
            {
                var testConfigurations = new[]
                {
                    new { Access = FileAccess.ReadWrite, Share = FileShare.Read },
                    new { Access = FileAccess.Write, Share = FileShare.Read },
                    new { Access = FileAccess.ReadWrite, Share = FileShare.None }
                };

                foreach (var config in testConfigurations)
                {
                    try
                    {
                        using (var stream = File.Open(filePath, FileMode.Open, config.Access, config.Share))
                        {
                            if (config.Access == FileAccess.Write || config.Access == FileAccess.ReadWrite)
                            {
                                UpdaterLogger.LogInfo($"Network file accessible with {config.Access}/{config.Share} on attempt {attempt}");
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        // Continue to next test configuration
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogInfo($"Alternative file access tests failed: {ex.Message}");
            }

            return false;
        }
    }
}