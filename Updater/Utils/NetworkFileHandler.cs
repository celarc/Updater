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

            UpdaterLogger.LogInfo("Network path detected - using simplified detection");

            // For network paths, use a much simpler and faster approach
            try
            {
                // Quick single test to see if file is accessible
                if (QuickNetworkFileTest(filePath))
                {
                    UpdaterLogger.LogInfo("Network file is available for writing");
                    return false;
                }

                UpdaterLogger.LogWarning("Network file appears to be in use");
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

        private static bool QuickNetworkFileTest(string filePath)
        {
            try
            {
                UpdaterLogger.LogInfo("Performing quick network file test");

                // Single, fast test - try to open with write access
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    // File is accessible for writing
                    return true;
                }
            }
            catch (IOException)
            {
                // File is locked by another process
                UpdaterLogger.LogInfo("Network file is locked by another process");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                // Access denied
                UpdaterLogger.LogWarning("Access denied to network file");
                return false;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Unexpected error testing network file: {ex.Message}");
                return false;
            }
        }
    }
}