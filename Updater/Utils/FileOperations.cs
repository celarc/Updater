using System;
using System.IO;

namespace Updater.Utils
{
    public static class FileOperations
    {
        public static bool IsFileInUse(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var targetDirectory = Path.GetDirectoryName(filePath);

            if (ProcessManager.IsApplicationRunningFromPath(fileName, targetDirectory) ||
                ProcessManager.IsApplicationRunningFromPath("BMC", targetDirectory))
            {
                return true;
            }

            return TryFileAccess(filePath);
        }

        private static bool TryFileAccess(string filePath)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        return false;
                    }
                }
                catch (IOException)
                {
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
            return true;
        }

        public static bool IsFileAccessible(string filePath)
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

        public static void ForceFileSystemFlush(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.Flush();
                }
                System.Threading.Thread.Sleep(100);
            }
            catch
            {
            }
        }

        public static void CreateBackupFile(string originalFile)
        {
            try
            {
                UpdaterLogger.CleanupOldBackupFiles(originalFile);
                var backupFile = originalFile + ".backup." + DateTime.Now.Ticks;
                File.Move(originalFile, backupFile);
                UpdaterLogger.LogInfo($"Created backup file: {Path.GetFileName(backupFile)}");
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Failed to create backup for {originalFile}: {ex.Message}", ex);
                throw;
            }
        }
    }
}