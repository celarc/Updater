using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Updater.Utils
{
    public static class ProcessManager
    {
        public static bool IsApplicationRunning(string processName)
        {
            return Process.GetProcessesByName(processName).Any();
        }

        public static bool IsApplicationRunningFromPath(string processName, string targetPath)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        var processPath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            var processDirectory = Path.GetDirectoryName(processPath);
                            if (string.Equals(processDirectory, targetPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            {
                                UpdaterLogger.LogInfo($"Found {processName} process running from target path: {processPath}");
                                return true;
                            }
                            else
                            {
                                UpdaterLogger.LogInfo($"{processName} process running from different path: {processPath} (target: {targetPath})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not check {processName} process path: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Error checking if {processName} is running from path: {ex.Message}");
                return false;
            }
        }

        public static void StopRunningApplications(string processName)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Could not stop {processName} process: {ex.Message}");
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public static void StopApplicationsFromPath(string processName, string targetPath)
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    try
                    {
                        var processPath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(processPath))
                        {
                            var processDirectory = Path.GetDirectoryName(processPath);
                            if (string.Equals(processDirectory, targetPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            {
                                UpdaterLogger.LogInfo($"Stopping {processName} process from target path: {processPath}");
                                process.Kill();
                                process.WaitForExit(5000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not stop {processName} process: {ex.Message}");
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Error stopping {processName} processes from path: {ex.Message}");
            }
        }
    }
}