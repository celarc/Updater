using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Updater.Configuration;

namespace Updater.Utils
{
    public static class UpdaterLogger
    {
        private static readonly object _lock = new object();

        public static void LogInfo(string message)
        {
            WriteLog("INFO", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public static void LogWarning(string message)
        {
            WriteLog("WARNING", message);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    var logPath = GetLogPath();
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var logEntry = $"{timestamp} [{level}] {message}{Environment.NewLine}";

                    File.AppendAllText(logPath, logEntry);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to write log: {ex.Message}");
            }
        }

        private static string GetLogPath()
        {
            try
            {
                var config = UpdaterConfig.Instance;
                var bmcPath = config.BMCPath;

                if (!string.IsNullOrEmpty(bmcPath) && Directory.Exists(bmcPath))
                {
                    var logDir = Path.Combine(bmcPath, "Log");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    return Path.Combine(logDir, "updaterLogs.txt");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get log path: {ex.Message}");
            }

            return GetDebugLogPath();
        }

        private static string GetDebugLogPath()
        {
            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var logDir = Path.Combine(appDir, "Log");

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }

            return Path.Combine(logDir, "updater.log");
        }

        public static async Task WriteUpdateLogAsync(string message, string targetPath)
        {
            await Task.Run(() =>
            {
                try
                {
                    var logDir = Path.Combine(targetPath, "Log");
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }

                    var logFile = Path.Combine(logDir, "AutoUpdate.txt");
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var logEntry = $"{timestamp}: {message}{Environment.NewLine}";

                    File.AppendAllText(logFile, logEntry);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to write update log: {ex.Message}");
                }
            });
        }

        public static void CleanupOldBackupFiles(string originalFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(originalFilePath))
                    return;

                var directory = Path.GetDirectoryName(originalFilePath);
                var fileName = Path.GetFileName(originalFilePath);
                var backupPattern = fileName + ".backup.*";

                if (directory == null)
                    return;

                var backupFiles = Directory.GetFiles(directory, backupPattern)
                    .Where(f => f != originalFilePath)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (backupFiles.Count == 0)
                    return;

                var filesToDelete = backupFiles.ToList();

                foreach (var fileToDelete in filesToDelete)
                {
                    try
                    {
                        File.Delete(fileToDelete.FullName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to delete backup file {fileToDelete.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to cleanup backup files: {ex.Message}");
            }
        }
    }
}