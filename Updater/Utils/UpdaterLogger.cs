using System;
using System.IO;
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
                    // Write to BMC log directory
                    var logPath = GetLogPath();
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var logEntry = $"{timestamp} [{level}] {message}{Environment.NewLine}";

                    File.AppendAllText(logPath, logEntry);

                    // Also write to updater's own directory for debugging
                    var debugLogPath = GetDebugLogPath();
                    if (debugLogPath != logPath)
                    {
                        File.AppendAllText(debugLogPath, logEntry);
                    }
                }
            }
            catch
            {
                // Silently fail if logging doesn't work
            }
        }

        private static string GetLogPath()
        {
            try
            {
                // Use BMC installation path from config
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
            catch
            {
                // Fall back to debug log if config fails
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
                catch
                {
                    // Silently fail if logging doesn't work
                }
            });
        }
    }
}