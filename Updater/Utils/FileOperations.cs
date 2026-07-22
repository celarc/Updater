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

        /// <summary>
        /// Poskrbi, da je datoteko mogoče prepisati. Če je v uporabi, najprej poskusi
        /// zapreti procese, ki jo držijo (BMC iz ciljne mape oz. istoimenski proces);
        /// če to ne uspe, jo preimenuje v .backup datoteko, da prenos lahko nadaljuje.
        /// Vrne false šele, ko ne uspe niti eno niti drugo. Ista kaskada, kot jo
        /// FTP prenos uporablja v FtpUpdateService.DownloadFile - izvlečena za HTTP prenos.
        /// </summary>
        public static bool EnsureFileWritable(string filePath)
        {
            if (!File.Exists(filePath) || !IsFileInUse(filePath))
                return true;

            UpdaterLogger.LogInfo($"File {Path.GetFileName(filePath)} is in use, attempting to free it");

            if (TryKillProcessesUsingFile(filePath))
            {
                UpdaterLogger.LogInfo($"Successfully freed file: {Path.GetFileName(filePath)}");
                return true;
            }

            try
            {
                CreateBackupFile(filePath);
                UpdaterLogger.LogInfo($"Created backup for locked file: {Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not rename locked file: {Path.GetFileName(filePath)} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Poskusi sprostiti zaklenjeno datoteko z zapiranjem procesov: najprej BMC procese,
        /// ki tečejo iz mape datoteke, nato še procese z imenom datoteke. Po vsakem koraku
        /// počaka 2 s in preveri, ali je datoteka sproščena. (Kopija logike iz FtpUpdateService.)
        /// </summary>
        private static bool TryKillProcessesUsingFile(string filePath)
        {
            try
            {
                UpdaterLogger.LogInfo($"Attempting to free file: {filePath}");

                var targetDirectory = Path.GetDirectoryName(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                if (ProcessManager.IsApplicationRunningFromPath("BMC", targetDirectory))
                {
                    UpdaterLogger.LogInfo("Stopping BMC processes from target directory");
                    ProcessManager.StopApplicationsFromPath("BMC", targetDirectory);

                    System.Threading.Thread.Sleep(2000);

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    if (!IsFileInUse(filePath))
                    {
                        UpdaterLogger.LogInfo("File is now available after stopping processes");
                        return true;
                    }
                }

                if (ProcessManager.IsApplicationRunning(fileName))
                {
                    UpdaterLogger.LogInfo($"Stopping {fileName} processes");
                    ProcessManager.StopRunningApplications(fileName);

                    System.Threading.Thread.Sleep(2000);

                    if (!IsFileInUse(filePath))
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