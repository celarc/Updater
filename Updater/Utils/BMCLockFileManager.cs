using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Updater.Utils
{
    public class BMCLockFileManager
    {
        private readonly string _lockFilePath;
        private readonly Timer _refreshTimer;
        private bool _disposed = false;

        public static void CleanupStaleLockFiles(string directory)
        {
            try
            {
                var lockFiles = Directory.GetFiles(directory, "*.bmc-lock");
                var currentTime = DateTime.Now;

                foreach (var lockFile in lockFiles)
                {
                    try
                    {
                        var lines = File.ReadAllLines(lockFile);
                        if (lines.Length >= 3 && DateTime.TryParse(lines[2], out var timestamp))
                        {
                            if (currentTime - timestamp > TimeSpan.FromSeconds(60))
                            {
                                File.Delete(lockFile);
                                UpdaterLogger.LogInfo($"Cleaned up stale BMC lock file: {lockFile}");
                            }
                        }
                        else
                        {
                            File.Delete(lockFile);
                            UpdaterLogger.LogInfo($"Cleaned up invalid BMC lock file: {lockFile}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not process lock file {lockFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not cleanup stale lock files: {ex.Message}");
            }
        }

        public static bool CheckForActiveBMCLocks(string directory)
        {
            try
            {
                var lockFiles = Directory.GetFiles(directory, "*.bmc-lock");
                var currentTime = DateTime.Now;

                foreach (var lockFile in lockFiles)
                {
                    try
                    {
                        var lockContent = File.ReadAllText(lockFile);
                        var lines = lockContent.Split('\n');

                        if (lines.Length >= 3)
                        {
                            var processId = lines[0].Trim();
                            var machineName = lines[1].Trim();
                            var timestampStr = lines[2].Trim();

                            if (DateTime.TryParse(timestampStr, out var timestamp))
                            {
                                // Check if lock file is recent (within last 30 seconds)
                                if (currentTime - timestamp < TimeSpan.FromSeconds(30))
                                {
                                    UpdaterLogger.LogInfo($"Found recent BMC lock file from {machineName}, PID: {processId}");
                                    return true;
                                }
                                else
                                {
                                    UpdaterLogger.LogInfo($"Found stale BMC lock file from {machineName}, cleaning up");
                                    try { File.Delete(lockFile); } catch { }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not read lock file {lockFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not check for BMC lock files: {ex.Message}");
            }

            return false;
        }
    }
}