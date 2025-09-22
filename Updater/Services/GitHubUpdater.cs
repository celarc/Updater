using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.ComponentModel;
using System.Threading;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Updater.Models;
using Updater.Configuration;
using Updater.Utils;

namespace Updater
{
    public class GitHubUpdater
    {
        private readonly string GitHubApiUrl = Constants.GITHUB_API_URL;
        private readonly string PersonalAccessToken = UpdaterConfig.Instance.GitHubPersonalAccessToken;


        private List<string> GetNetworkMachinesInSubnet()
        {
            var machines = new List<string>();

            try
            {
                // Get local IP address to determine subnet
                var localIP = "";
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

                foreach (var ni in networkInterfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        var ipProps = ni.GetIPProperties();
                        foreach (var addr in ipProps.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                localIP = addr.Address.ToString();
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(localIP)) break;
                    }
                }

                if (!string.IsNullOrEmpty(localIP))
                {
                    // Extract subnet (assuming /24)
                    var parts = localIP.Split('.');
                    if (parts.Length == 4)
                    {
                        var subnet = $"{parts[0]}.{parts[1]}.{parts[2]}";

                        // Common machines in the same subnet
                        for (int i = 20; i <= 30; i++) // Adjust range as needed
                        {
                            machines.Add($"{subnet}.{i}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not determine network machines: {ex.Message}");
            }

            return machines;
        }

        private bool IsRemoteBMCRunning(string targetPath)
        {
            UpdaterLogger.LogInfo($"Checking for remote BMC processes accessing: {targetPath}");

            // Strategy 1: Check known machines in subnet (WMI-based, currently disabled)
            var machines = GetNetworkMachinesInSubnet();

            foreach (var machine in machines)
            {
                try
                {
                    if (CheckBMCProcessOnMachine(machine, targetPath))
                    {
                        UpdaterLogger.LogInfo($"Found BMC process on machine: {machine}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogInfo($"Could not check machine {machine}: {ex.Message}");
                }
            }

            // Strategy 2: Check for lock files created by BMC instances
            if (CheckForBMCLockFiles(targetPath))
            {
                UpdaterLogger.LogInfo("Found BMC lock files indicating active processes");
                return true;
            }

            return false;
        }

        private bool CheckBMCProcessOnMachine(string machineName, string targetPath)
        {
            UpdaterLogger.LogInfo($"WMI-based process detection temporarily disabled for machine {machineName}");
            return false;
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Failed to check administrator privileges: {ex.Message}");
                return false;
            }
        }

        private string GetOpenFilesExecutablePath()
        {
            try
            {
                // Check if we're running as a 32-bit process on a 64-bit system
                bool is64BitOS = Environment.Is64BitOperatingSystem;
                bool is64BitProcess = Environment.Is64BitProcess;

                UpdaterLogger.LogInfo($"System: {(is64BitOS ? "64-bit" : "32-bit")}, Process: {(is64BitProcess ? "64-bit" : "32-bit")}");

                if (is64BitOS && !is64BitProcess)
                {
                    // 32-bit process on 64-bit OS - use sysnative to access 64-bit system directory
                    string sysnativePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "sysnative", "openfiles.exe");
                    if (File.Exists(sysnativePath))
                    {
                        UpdaterLogger.LogInfo($"Using sysnative path for openfiles.exe: {sysnativePath}");
                        return sysnativePath;
                    }

                    // Fallback to System32 (will be redirected to SysWOW64, but try anyway)
                    string system32Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "openfiles.exe");
                    UpdaterLogger.LogInfo($"Sysnative not available, trying System32 path: {system32Path}");
                    return system32Path;
                }
                else
                {
                    // 64-bit process or 32-bit OS - use normal System32 path
                    string normalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "openfiles.exe");
                    UpdaterLogger.LogInfo($"Using normal system path for openfiles.exe: {normalPath}");
                    return normalPath;
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Error determining openfiles.exe path: {ex.Message}");
                return "openfiles.exe"; // Fallback to PATH resolution
            }
        }

        private bool CheckForOpenFilesInBMCDirectory(string targetPath)
        {
            try
            {
                UpdaterLogger.LogInfo($"Checking for open files in BMC directory: {Path.GetDirectoryName(targetPath)}");

                // Check if we have admin privileges
                bool isAdmin = IsRunningAsAdministrator();
                if (!isAdmin)
                {
                    UpdaterLogger.LogInfo("Updater is not running as administrator. Skipping OpenFiles detection.");
                    return false; // Skip OpenFiles entirely if not admin
                }

                // Determine the correct path to openfiles.exe based on system architecture
                string openFilesPath = GetOpenFilesExecutablePath();

                if (string.IsNullOrEmpty(openFilesPath))
                {
                    UpdaterLogger.LogWarning("Could not determine openfiles.exe path");
                    return false;
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = openFilesPath,
                        Arguments = "/query /fo csv",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    if (error.Contains("Access is denied"))
                    {
                        UpdaterLogger.LogWarning("OpenFiles requires administrator privileges. Run updater as administrator to detect network file usage.");
                        // Continue without OpenFiles detection rather than failing
                    }
                    else
                    {
                        UpdaterLogger.LogWarning($"OpenFiles command failed with exit code {process.ExitCode}: {error}");
                    }
                    return false;
                }

                if (string.IsNullOrEmpty(output))
                {
                    UpdaterLogger.LogInfo("No open files detected by OpenFiles command");
                    return false;
                }

                // Log raw output for debugging
                UpdaterLogger.LogInfo($"OpenFiles raw output length: {output.Length} characters");
                if (output.Length < 1000) // Only log if output is reasonably small
                {
                    UpdaterLogger.LogInfo($"OpenFiles raw output:\n{output}");
                }

                var bmcDirectory = Path.GetDirectoryName(targetPath);
                // Normalize the BMC directory path for comparison
                if (!bmcDirectory.EndsWith("\\"))
                    bmcDirectory += "\\";

                UpdaterLogger.LogInfo($"Checking for files in directory: {bmcDirectory}");

                var lines = output.Split('\n');
                bool foundOpenFiles = false;
                int lineCount = 0;

                foreach (var line in lines)
                {
                    lineCount++;
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header lines
                    if (line.StartsWith("\"Hostname\"") || line.StartsWith("INFO:") || line.Contains("-----"))
                        continue;

                    try
                    {
                        // Parse CSV format: "Hostname","ID","Accessed By","Type","Open File (Path\executable)"
                        // Note: Actual format has only 5 fields, not 7
                        var fields = ParseCsvLine(line);
                        UpdaterLogger.LogInfo($"Line {lineCount}: Parsed {fields.Length} fields from: {line.Substring(0, Math.Min(line.Length, 100))}");

                        if (fields.Length == 4)
                        {
                            // Local openfiles format: "ID","Accessed By","Type","Open File (Path\executable)"
                            var fileId = fields[0].Trim('"');
                            var accessedBy = fields[1].Trim('"');
                            var fileType = fields[2].Trim('"');
                            var openFilePath = fields[3].Trim('"');

                            UpdaterLogger.LogInfo($"Parsed: ID={fileId}, User={accessedBy}, Type={fileType}, Path={openFilePath}");

                            // Check if the open file is in the BMC directory
                            // Handle both file paths and directory paths
                            if (!string.IsNullOrEmpty(openFilePath))
                            {
                                // Normalize the path for comparison
                                string normalizedPath = openFilePath;
                                if (!normalizedPath.EndsWith("\\") && Directory.Exists(normalizedPath))
                                    normalizedPath += "\\";

                                if (normalizedPath.StartsWith(bmcDirectory, StringComparison.OrdinalIgnoreCase) ||
                                    normalizedPath.Equals(bmcDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                                {
                                    UpdaterLogger.LogInfo($"Found open file in BMC directory: {openFilePath} (accessed by: {accessedBy}, ID: {fileId})");
                                    foundOpenFiles = true;
                                }
                            }
                        }
                        else if (fields.Length > 0)
                        {
                            UpdaterLogger.LogInfo($"Unexpected field count {fields.Length} for line: {line}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not parse OpenFiles line: {line} - {ex.Message}");
                    }
                }

                return foundOpenFiles;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"OpenFiles detection failed: {ex.Message}");
                return false;
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private bool CheckForBMCLockFiles(string targetPath)
        {
            try
            {
                var directory = Path.GetDirectoryName(targetPath);
                var lockFiles = Directory.GetFiles(directory, "*.bmc-lock");

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
                                if (DateTime.Now - timestamp < TimeSpan.FromSeconds(30))
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


        private bool CheckForBMCResponseFiles(string directory)
        {
            try
            {
                var responseFiles = Directory.GetFiles(directory, ".bmc-response-*");
                var currentTime = DateTime.Now;

                foreach (var responseFile in responseFiles)
                {
                    try
                    {
                        var lines = File.ReadAllLines(responseFile);
                        if (lines.Length >= 3)
                        {
                            // Extract timestamp from response file
                            var timestampLine = lines[2].Replace("Timestamp: ", "");
                            if (DateTime.TryParse(timestampLine, out var timestamp))
                            {
                                // Check if response file is recent (within last 45 seconds)
                                if (currentTime - timestamp < TimeSpan.FromSeconds(45))
                                {
                                    UpdaterLogger.LogInfo($"Found recent BMC response file: {responseFile}");
                                    return true;
                                }
                                else
                                {
                                    // Clean up old response files
                                    try { File.Delete(responseFile); } catch { }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not process response file {responseFile}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not check for BMC response files: {ex.Message}");
            }

            return false;
        }


        private bool IsFileInUse(string filePath)
        {
            if (!File.Exists(filePath))
                return false;

            // First check for open files using OpenFiles command
            UpdaterLogger.LogInfo("Checking for open files via OpenFiles command");
            if (CheckForOpenFilesInBMCDirectory(filePath))
            {
                UpdaterLogger.LogWarning("Open files detected in BMC directory via OpenFiles - FILE IS IN USE");
                return true;
            }

            // Use our simplified network/local file handling
            return NetworkFileHandler.IsNetworkFileInUse(filePath);
        }

        private string GetFileUsageDiagnostic(string filePath)
        {
            if (!File.Exists(filePath))
                return "File does not exist";

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var processes = Process.GetProcessesByName(fileName);
            if (processes.Length == 0)
            {
                processes = Process.GetProcessesByName("BMC");
            }

            if (processes.Length > 0)
            {
                var processNames = string.Join(", ", processes.Select(p => $"{p.ProcessName} (PID: {p.Id})"));
                return $"File is in use by BMC processes: {processNames}";
            }

            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Write, FileShare.Read))
                {
                    return "File is available for writing";
                }
            }
            catch (IOException ex)
            {
                return $"File is locked by another process: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                return $"Access denied to file: {ex.Message}";
            }
        }

        private bool TryKillProcessesUsingFile(string filePath, IProgress<UpdateProgress> progress = null)
        {
            try
            {
                var isNetworkPath = NetworkFileHandler.IsNetworkPath(filePath);
                UpdaterLogger.LogInfo($"Attempting to handle file in use: {filePath} (Network path: {isNetworkPath})");

                // First, always try to force close open file handles using OpenFiles (works for network shares accessing local files)
                UpdaterLogger.LogInfo("Attempting to force close open file handles via OpenFiles");
                if (TryForceCloseOpenFiles(filePath, progress))
                {
                    UpdaterLogger.LogInfo("Successfully closed open file handles via OpenFiles");
                    Thread.Sleep(2000); // Give time for handles to release

                    // Check if file is now available
                    if (!IsFileInUse(filePath))
                    {
                        UpdaterLogger.LogInfo("File is now available after closing open handles");
                        return true;
                    }
                }

                // For network paths, use additional replacement strategies
                if (isNetworkPath)
                {
                    return TryNetworkFileReplacement(filePath, progress);
                }

                // Local process handling (existing logic)
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var processes = Process.GetProcessesByName(fileName);

                if (processes.Length == 0)
                {
                    processes = Process.GetProcessesByName("BMC");
                    if (processes.Length == 0)
                    {
                        processes = Process.GetProcessesByName("WebParam");
                    }
                }

                bool anyProcessKilled = false;
                foreach (var process in processes)
                {
                    try
                    {
                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.KillingProcess, process.ProcessName, process.Id)));
                        UpdaterLogger.LogInfo($"Killing process: {process.ProcessName} (PID: {process.Id})");

                        if (!process.HasExited)
                        {
                            process.Kill();
                            anyProcessKilled = true;

                            // Wait longer for process to fully exit
                            bool exited = process.WaitForExit(10000); // Wait up to 10 seconds
                            if (!exited)
                            {
                                progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ProcessDidNotExit, process.ProcessName)));
                                UpdaterLogger.LogWarning($"Process {process.ProcessName} did not exit within timeout");
                                continue;
                            }
                        }

                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ProcessTerminatedSuccessfully, process.ProcessName)));
                        UpdaterLogger.LogInfo($"Process {process.ProcessName} terminated successfully");
                    }
                    catch (Exception ex)
                    {
                        progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.FailedToKillProcess, process.ProcessName, ex.Message)));
                        UpdaterLogger.LogError($"Failed to kill process {process.ProcessName}", ex);
                    }
                    finally
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch { }
                    }
                }

                if (anyProcessKilled)
                {
                    // Wait longer for file handles to be released after killing processes
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.WaitingForFileHandles));
                    UpdaterLogger.LogInfo("Waiting for file handles to be released");
                    System.Threading.Thread.Sleep(3000);
                }

                // Force garbage collection to help release any remaining handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Check if file is still in use after killing processes
                var stillInUse = IsFileInUse(filePath);
                if (stillInUse)
                {
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FileStillInUse));
                    UpdaterLogger.LogWarning("File is still in use after process termination");
                }
                else
                {
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FileNowAvailable));
                    UpdaterLogger.LogInfo("File is now available for replacement");
                }

                return !stillInUse;
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.Create(0, string.Format(SlovenianMessages.ErrorKillingProcesses, ex.Message)));
                UpdaterLogger.LogError("Error while trying to kill processes", ex);
                return false;
            }
        }

        private bool TryNetworkFileReplacement(string filePath, IProgress<UpdateProgress> progress = null)
        {
            UpdaterLogger.LogInfo($"Attempting bulletproof network file replacement for: {filePath}");

            var config = UpdaterConfig.Instance;

            // Strategy 1: Try to terminate remote BMC processes first
            if (TryTerminateRemoteBMCProcesses(filePath, progress))
            {
                UpdaterLogger.LogInfo("Successfully terminated remote BMC processes");

                // Wait a bit for processes to fully close and file handles to be released
                progress?.Report(UpdateProgress.Create(0, "Waiting for remote processes to fully close..."));
                System.Threading.Thread.Sleep(3000);

                if (!IsFileInUse(filePath))
                {
                    return true;
                }
            }

            // Strategy 2: Enhanced signal mechanism with multiple signal types
            if (TryEnhancedSignalMechanism(filePath, progress))
            {
                UpdaterLogger.LogInfo("Remote processes responded to enhanced signals");
                return true;
            }

            // Strategy 3: Wait and retry multiple times for network file to become available
            var maxWaitAttempts = config.NetworkFileRetryAttempts;
            var waitDelay = config.NetworkFileRetryDelayMs;

            for (int attempt = 1; attempt <= maxWaitAttempts; attempt++)
            {
                progress?.Report(UpdateProgress.Create(0, $"Checking network file availability (attempt {attempt}/{maxWaitAttempts})"));
                UpdaterLogger.LogInfo($"Network file availability check attempt {attempt}/{maxWaitAttempts}");

                if (!IsFileInUse(filePath))
                {
                    UpdaterLogger.LogInfo($"Network file became available on attempt {attempt}");
                    progress?.Report(UpdateProgress.Create(0, "Network file is now available for replacement"));
                    return true;
                }

                if (attempt < maxWaitAttempts)
                {
                    progress?.Report(UpdateProgress.Create(0, $"Network file still locked, waiting {waitDelay}ms..."));
                    UpdaterLogger.LogInfo($"Network file still locked, waiting {waitDelay}ms before retry");
                    System.Threading.Thread.Sleep(waitDelay);
                }
            }

            // If all strategies fail, we'll return false and let the caller handle file renaming
            UpdaterLogger.LogWarning("All bulletproof network file replacement strategies failed");
            progress?.Report(UpdateProgress.Create(0, "Network file remains locked, will attempt backup and replace"));
            return false;
        }

        private bool TryForceCloseOpenFiles(string targetPath, IProgress<UpdateProgress> progress = null)
        {
            try
            {
                UpdaterLogger.LogInfo($"Attempting to force close open files in BMC directory: {Path.GetDirectoryName(targetPath)}");
                progress?.Report(UpdateProgress.Create(0, "Scanning for open files in BMC directory"));

                // Determine the correct path to openfiles.exe
                string openFilesPath = GetOpenFilesExecutablePath();
                if (string.IsNullOrEmpty(openFilesPath))
                {
                    UpdaterLogger.LogWarning("Could not determine openfiles.exe path");
                    return false;
                }

                var bmcDirectory = Path.GetDirectoryName(targetPath);
                var fileIdsToClose = new List<string>();
                var sessionIdsToClose = new HashSet<string>();

                // First, get list of open files in BMC directory
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = openFilesPath,
                        Arguments = "/query /fo csv",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    if (error.Contains("Access is denied"))
                    {
                        UpdaterLogger.LogWarning("OpenFiles requires administrator privileges. Cannot force close files without admin rights.");
                        progress?.Report(UpdateProgress.Create(0, "Administrator privileges required for force close"));
                    }
                    else
                    {
                        UpdaterLogger.LogWarning($"OpenFiles query failed with exit code {process.ExitCode}: {error}");
                    }
                    return false;
                }

                if (string.IsNullOrEmpty(output))
                {
                    UpdaterLogger.LogInfo("No open files found");
                    return true; // No files to close means success
                }

                // Normalize the BMC directory path for comparison
                if (!bmcDirectory.EndsWith("\\"))
                    bmcDirectory += "\\";

                UpdaterLogger.LogInfo($"Looking for files to close in directory: {bmcDirectory}");

                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header lines
                    if (line.StartsWith("\"Hostname\"") || line.StartsWith("INFO:") || line.Contains("-----"))
                        continue;

                    try
                    {
                        // Parse CSV format: "Hostname","ID","Accessed By","Type","Open File (Path\executable)"
                        var fields = ParseCsvLine(line);
                        if (fields.Length == 4)
                        {
                            // Local openfiles format: "ID","Accessed By","Type","Open File (Path\executable)"
                            var fileId = fields[0].Trim('"');
                            var accessedBy = fields[1].Trim('"');
                            var fileType = fields[2].Trim('"');
                            var openFilePath = fields[3].Trim('"');

                            if (!string.IsNullOrEmpty(openFilePath))
                            {
                                // Normalize the path for comparison
                                string normalizedPath = openFilePath;
                                if (!normalizedPath.EndsWith("\\") && Directory.Exists(normalizedPath))
                                    normalizedPath += "\\";

                                if (normalizedPath.StartsWith(bmcDirectory, StringComparison.OrdinalIgnoreCase) ||
                                    normalizedPath.Equals(bmcDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                                {
                                    UpdaterLogger.LogInfo($"Found open BMC file to close: {openFilePath} (ID: {fileId}, User: {accessedBy})");
                                    fileIdsToClose.Add(fileId);

                                    // Extract session ID from accessedBy if it contains session info
                                    if (!string.IsNullOrEmpty(accessedBy) && accessedBy != "N/A")
                                    {
                                        sessionIdsToClose.Add(accessedBy);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Could not parse OpenFiles line for closing: {line} - {ex.Message}");
                    }
                }

                if (fileIdsToClose.Count == 0)
                {
                    UpdaterLogger.LogInfo("No open BMC files found to close");
                    return true;
                }

                // Try to disconnect specific file IDs first
                bool anyFilesClosed = false;
                foreach (var fileId in fileIdsToClose)
                {
                    try
                    {
                        progress?.Report(UpdateProgress.Create(0, $"Disconnecting file ID: {fileId}"));
                        UpdaterLogger.LogInfo($"Attempting to disconnect file ID: {fileId}");

                        var closeProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = openFilesPath,
                                Arguments = $"/disconnect /id {fileId}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                CreateNoWindow = true
                            }
                        };

                        closeProcess.Start();
                        var closeOutput = closeProcess.StandardOutput.ReadToEnd();
                        var closeError = closeProcess.StandardError.ReadToEnd();
                        closeProcess.WaitForExit();

                        if (closeProcess.ExitCode == 0)
                        {
                            UpdaterLogger.LogInfo($"Successfully disconnected file ID: {fileId}");
                            anyFilesClosed = true;
                        }
                        else
                        {
                            UpdaterLogger.LogWarning($"Failed to disconnect file ID {fileId}: {closeError}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdaterLogger.LogWarning($"Error disconnecting file ID {fileId}: {ex.Message}");
                    }
                }

                // If specific file disconnection failed, try to disconnect by accessed by (session)
                if (!anyFilesClosed && sessionIdsToClose.Count > 0)
                {
                    foreach (var sessionId in sessionIdsToClose)
                    {
                        try
                        {
                            progress?.Report(UpdateProgress.Create(0, $"Disconnecting session: {sessionId}"));
                            UpdaterLogger.LogInfo($"Attempting to disconnect session: {sessionId}");

                            var closeProcess = new Process
                            {
                                StartInfo = new ProcessStartInfo
                                {
                                    FileName = openFilesPath,
                                    Arguments = $"/disconnect /a \"{sessionId}\"",
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true,
                                    RedirectStandardError = true,
                                    CreateNoWindow = true
                                }
                            };

                            closeProcess.Start();
                            var closeOutput = closeProcess.StandardOutput.ReadToEnd();
                            var closeError = closeProcess.StandardError.ReadToEnd();
                            closeProcess.WaitForExit();

                            if (closeProcess.ExitCode == 0)
                            {
                                UpdaterLogger.LogInfo($"Successfully disconnected session: {sessionId}");
                                anyFilesClosed = true;
                            }
                            else
                            {
                                UpdaterLogger.LogWarning($"Failed to disconnect session {sessionId}: {closeError}");
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdaterLogger.LogWarning($"Error disconnecting session {sessionId}: {ex.Message}");
                        }
                    }
                }

                return anyFilesClosed;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Force close open files failed: {ex.Message}");
                return false;
            }
        }

        private bool TryTerminateRemoteBMCProcesses(string targetPath, IProgress<UpdateProgress> progress = null)
        {
            UpdaterLogger.LogInfo("Attempting to terminate remote BMC processes using OpenFiles force disconnect");
            progress?.Report(UpdateProgress.Create(0, "Checking for open BMC files and disconnecting remote sessions"));

            // Strategy 1: Force disconnect open file handles using OpenFiles
            bool fileHandlesClosed = TryForceCloseOpenFiles(targetPath, progress);

            // Strategy 2: Use signal mechanism as fallback
            if (!fileHandlesClosed)
            {
                UpdaterLogger.LogInfo("OpenFiles force disconnect failed - using signal mechanism instead");
                progress?.Report(UpdateProgress.Create(0, "Force disconnect failed - using signal mechanism instead"));
                return false;
            }

            return fileHandlesClosed;
        }

        private bool TryEnhancedSignalMechanism(string filePath, IProgress<UpdateProgress> progress = null)
        {
            UpdaterLogger.LogInfo("Attempting enhanced signal mechanism");
            var config = UpdaterConfig.Instance;

            try
            {
                var directory = Path.GetDirectoryName(filePath);

                // Create multiple signal files for different purposes
                var urgentSignalFile = Path.Combine(directory, ".updater-urgent-close-bmc");
                var politeSignalFile = Path.Combine(directory, ".updater-please-close-bmc");
                var forceSignalFile = Path.Combine(directory, ".updater-force-close-bmc");

                var timestamp = DateTime.Now;
                var signalContent = $"BMC Update in progress - Please close all BMC instances\n" +
                                  $"Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                                  $"Updater Machine: {Environment.MachineName}\n" +
                                  $"Update Type: GitHub Download\n" +
                                  $"Priority: HIGH";

                // Create all signal files
                progress?.Report(UpdateProgress.Create(0, "Creating enhanced close signals for remote processes"));
                File.WriteAllText(politeSignalFile, $"POLITE REQUEST\n{signalContent}");
                System.Threading.Thread.Sleep(500);
                File.WriteAllText(urgentSignalFile, $"URGENT REQUEST\n{signalContent}");
                System.Threading.Thread.Sleep(500);
                File.WriteAllText(forceSignalFile, $"FORCE CLOSE REQUEST\n{signalContent}");

                var signalAttempts = config.NetworkFileSignalWaitAttempts;
                var signalDelay = config.NetworkFileSignalWaitDelayMs;

                // Wait with progressive urgency
                for (int attempt = 1; attempt <= signalAttempts; attempt++)
                {
                    var waitTime = signalDelay + (attempt * 500); // Increase wait time each attempt
                    System.Threading.Thread.Sleep(waitTime);

                    progress?.Report(UpdateProgress.Create(0, $"Waiting for remote processes to respond to enhanced signals (attempt {attempt}/{signalAttempts})"));

                    if (!IsFileInUse(filePath))
                    {
                        UpdaterLogger.LogInfo("Remote processes responded to enhanced signals");

                        // Clean up signal files
                        try { File.Delete(politeSignalFile); } catch { }
                        try { File.Delete(urgentSignalFile); } catch { }
                        try { File.Delete(forceSignalFile); } catch { }

                        return true;
                    }

                    // Add more signal files with increasing urgency
                    if (attempt == 2)
                    {
                        var criticalSignalFile = Path.Combine(directory, ".updater-critical-close-bmc");
                        File.WriteAllText(criticalSignalFile, $"CRITICAL - IMMEDIATE CLOSE REQUIRED\n{signalContent}");
                    }
                }

                // Clean up signal files
                try { File.Delete(politeSignalFile); } catch { }
                try { File.Delete(urgentSignalFile); } catch { }
                try { File.Delete(forceSignalFile); } catch { }
                try { File.Delete(Path.Combine(directory, ".updater-critical-close-bmc")); } catch { }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Enhanced signal mechanism failed: {ex.Message}");
            }

            return false;
        }

        public async Task<UpdateResult> UpdateFromGitHubAsync(GitHubRelease release, string targetPath,
            IProgress<UpdateProgress> progress = null)
        {
            try
            {
                if (release?.Assets == null || !release.Assets.Any())
                {
                    return UpdateResult.CreateFailure("No assets found in the selected release");
                }

                var totalAssets = release.Assets.Count;
                var downloadedAssets = 0;

                progress?.Report(UpdateProgress.Create(0, SlovenianMessages.StartingGitHubDownload));
                UpdaterLogger.LogInfo("Starting GitHub download");

                // Ensure target directory exists
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                foreach (var asset in release.Assets)
                {
                    try
                    {
                        var fileName = asset.Name;
                        var targetFile = Path.Combine(targetPath, fileName);

                        progress?.Report(UpdateProgress.Create(
                            (downloadedAssets * 100) / totalAssets,
                            string.Format(SlovenianMessages.Downloading, fileName), fileName));

                        // Check if file is in use before downloading
                        if (IsFileInUse(targetFile))
                        {
                            var diagnostic = GetFileUsageDiagnostic(targetFile);
                            progress?.Report(UpdateProgress.Create(
                                (downloadedAssets * 100) / totalAssets,
                                string.Format(SlovenianMessages.FileInUse, fileName, diagnostic)));

                            if (!TryKillProcessesUsingFile(targetFile, progress))
                            {
                                // Last resort: try to rename the existing file and then download
                                progress?.Report(UpdateProgress.Create(
                                    (downloadedAssets * 100) / totalAssets,
                                    string.Format(SlovenianMessages.AttemptingToRename, fileName)));

                                try
                                {
                                    FileOperations.CreateBackupFile(targetFile);
                                    progress?.Report(UpdateProgress.Create(
                                        (downloadedAssets * 100) / totalAssets,
                                        string.Format(SlovenianMessages.ExistingFileRenamed, "backup file")));
                                }
                                catch (Exception ex)
                                {
                                    var finalDiagnostic = GetFileUsageDiagnostic(targetFile);
                                    var errorMsg = string.Format(SlovenianMessages.CouldNotDownload, fileName, finalDiagnostic, ex.Message);
                                    UpdaterLogger.LogError(errorMsg);
                                    return UpdateResult.CreateFailure(errorMsg);
                                }
                            }
                            else
                            {
                                progress?.Report(UpdateProgress.Create(
                                    (downloadedAssets * 100) / totalAssets,
                                    SlovenianMessages.ProcessesKilledSuccessfully));
                            }
                        }

                        using (var webClient = new WebClient())
                        {
                            webClient.Headers.Add("User-Agent", "Updater");
                            webClient.Headers.Add("Accept", "application/octet-stream");

                            if (!string.IsNullOrEmpty(PersonalAccessToken))
                            {
                                webClient.Headers.Add("Authorization", $"Bearer {PersonalAccessToken}");
                            }

                            string downloadUrl;
                            if (!string.IsNullOrEmpty(PersonalAccessToken))
                            {
                                downloadUrl = $"https://api.github.com/repos/celarc/BMC/releases/assets/{asset.Id}";
                            }
                            else
                            {
                                downloadUrl = asset.BrowserDownloadUrl;
                            }

                            // Add progress reporting for individual file download
                            var lastProgressTime = DateTime.Now;
                            var lastBytesReceived = 0L;

                            webClient.DownloadProgressChanged += (sender, e) =>
                            {
                                var currentTime = DateTime.Now;
                                var timeDiff = currentTime - lastProgressTime;

                                // Update progress every 250ms to avoid spam
                                if (timeDiff.TotalMilliseconds > 250)
                                {
                                    var bytesDiff = e.BytesReceived - lastBytesReceived;
                                    var speed = timeDiff.TotalSeconds > 0 ? bytesDiff / timeDiff.TotalSeconds : 0;

                                    // Calculate progress: base from completed assets + current asset progress
                                    var baseProgress = totalAssets > 0 ? (downloadedAssets * 100) / totalAssets : 0;
                                    var currentAssetProgress = e.TotalBytesToReceive > 0 ?
                                        (int)((e.BytesReceived * 100) / e.TotalBytesToReceive) : 0;
                                    var assetProgressContribution = totalAssets > 0 ? currentAssetProgress / totalAssets : currentAssetProgress;
                                    var overallProgress = Math.Min(100, baseProgress + assetProgressContribution);

                                    progress?.Report(UpdateProgress.CreateWithDetails(
                                        overallProgress,
                                        $"Prenašam: {fileName} ({(double)e.BytesReceived / e.TotalBytesToReceive:P0})",
                                        fileName,
                                        e.BytesReceived,
                                        e.TotalBytesToReceive,
                                        speed,
                                        "Prenašam"
                                    ));

                                    lastProgressTime = currentTime;
                                    lastBytesReceived = e.BytesReceived;
                                }
                            };

                            Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                            await webClient.DownloadFileTaskAsync(downloadUrl, targetFile);
                        }

                        // Verify the download was successful
                        if (!File.Exists(targetFile))
                        {
                            var errorMsg = $"Download failed: {fileName} was not created";
                            UpdaterLogger.LogError(errorMsg);
                            return UpdateResult.CreateFailure(errorMsg);
                        }

                        var fileInfo = new FileInfo(targetFile);
                        if (fileInfo.Length == 0)
                        {
                            var errorMsg = $"Download failed: {fileName} is empty";
                            UpdaterLogger.LogError(errorMsg);
                            return UpdateResult.CreateFailure(errorMsg);
                        }

                        downloadedAssets++;
                        var percent = (downloadedAssets * 100) / totalAssets;
                        progress?.Report(UpdateProgress.Create(percent,
                            string.Format(SlovenianMessages.Downloaded, fileName), fileName));
                        UpdaterLogger.LogInfo($"Downloaded: {fileName} ({fileInfo.Length:N0} bytes)");
                    }
                    catch (Exception ex)
                    {
                        // Stop downloading other files if any download fails
                        var errorMsg = string.Format(SlovenianMessages.DownloadFailed, asset.Name, ex.Message);
                        UpdaterLogger.LogError(errorMsg, ex);
                        return UpdateResult.CreateFailure(errorMsg, ex);
                    }
                }

                // Force file system sync to ensure all files are fully written
                progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FinalizingTransfer));
                UpdaterLogger.LogInfo("Forcing file system sync to ensure all GitHub files are written");

                // Force garbage collection to release any file handles
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Extended delay for GitHub downloads to ensure file system stability
                // GitHub downloads may need more time than FTP for files to be fully accessible
                progress?.Report(UpdateProgress.Create(100, "Finalizing file system operations..."));
                UpdaterLogger.LogInfo("Waiting for GitHub downloaded files to be fully accessible");
                System.Threading.Thread.Sleep(3000); // Increased from 1000ms to 3000ms

                // Additional verification that key files are accessible
                var mainExecutable = Path.Combine(targetPath, "BMC.exe");
                if (File.Exists(mainExecutable))
                {
                    // Try to ensure the main executable is fully written and accessible
                    for (int i = 0; i < 5; i++)
                    {
                        try
                        {
                            using (var stream = File.Open(mainExecutable, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                if (stream.Length > 0)
                                {
                                    UpdaterLogger.LogInfo("BMC.exe is accessible and ready for version detection");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdaterLogger.LogWarning($"BMC.exe not yet accessible (attempt {i + 1}/5): {ex.Message}");
                            if (i < 4) System.Threading.Thread.Sleep(1000);
                        }
                    }
                }

                var successMsg = string.Format(SlovenianMessages.DownloadedFiles, downloadedAssets, totalAssets);
                UpdaterLogger.LogInfo(successMsg);
                return UpdateResult.CreateSuccess(successMsg, downloadedAssets);
            }
            catch (Exception ex)
            {
                progress?.Report(UpdateProgress.CreateError(ex));
                var errorMsg = string.Format(SlovenianMessages.GitHubDownloadFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        public async Task<List<GitHubRelease>> GetReleases()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Updater");
                    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    if (!string.IsNullOrEmpty(PersonalAccessToken))
                    {
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue(
                                "Bearer",
                                PersonalAccessToken
                            );
                    }

                    var response = await httpClient.GetAsync(GitHubApiUrl);

                    var content = await response.Content.ReadAsStringAsync();

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception("Repository not found or no releases exist. Please verify:");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"GitHub API error: {response.StatusCode}\n{content}");
                    }

                    var releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(content);

                    if (releases == null || releases.Count == 0)
                    {
                        throw new Exception("API returned empty releases list despite success status");
                    }

                    return releases;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string Verzija { get; set; }

        [JsonProperty("name")]
        public string Ime { get; set; }

        [JsonProperty("published_at")]
        public DateTime Objavljeno { get; set; }

        [JsonProperty("body")]
        public string Opomba { get; set; }

        [JsonProperty("assets")]
        public List<GitHubAsset> Assets { get; set; } = new List<GitHubAsset>();
    }

    public class GitHubAsset
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }

        [JsonProperty("download_count")]
        public int DownloadCount { get; set; }

        [JsonProperty("created_at")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}