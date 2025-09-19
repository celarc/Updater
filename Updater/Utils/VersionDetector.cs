using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Updater.Models;

namespace Updater.Utils
{
    public static class VersionDetector
    {
        public static async Task<VersionInfo> GetCurrentVersionAsync(string applicationPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var exePath = Path.Combine(applicationPath, Constants.BMC_EXECUTABLE);

                    if (!File.Exists(exePath))
                    {
                        UpdaterLogger.LogWarning($"BMC.exe not found at: {exePath}");
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    FileOperations.ForceFileSystemFlush(exePath);

                    if (!FileOperations.IsFileAccessible(exePath))
                    {
                        UpdaterLogger.LogWarning($"BMC.exe is locked or not accessible: {exePath}");
                        return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                    }

                    return ExtractVersionInfo(exePath);
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogError("Failed to get current version", ex);
                    return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
                }
            });
        }

        private static VersionInfo ExtractVersionInfo(string exePath)
        {
            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(exePath);
                UpdaterLogger.LogInfo($"File version info obtained for: {exePath}");

                var productVersion = versionInfo.ProductVersion ?? "";
                var fileVersion = versionInfo.FileVersion ?? "";
                var comments = versionInfo.Comments ?? "";

                var assemblyResult = TryGetAssemblyVersionInfo(exePath, fileVersion);
                if (assemblyResult != null && assemblyResult.Channel != "Unknown")
                {
                    UpdaterLogger.LogInfo($"Version extracted from assembly: {assemblyResult.DisplayVersion}");
                    return assemblyResult;
                }

                if (TryParseVersionFromString(productVersion, out var parsed))
                {
                    UpdaterLogger.LogInfo($"Version extracted from ProductVersion: {parsed.DisplayVersion}");
                    return parsed;
                }

                if (TryParseVersionFromString(comments, out parsed))
                {
                    UpdaterLogger.LogInfo($"Version extracted from Comments: {parsed.DisplayVersion}");
                    return parsed;
                }

                if (TryParseVersionFromString(fileVersion, out parsed))
                {
                    UpdaterLogger.LogInfo($"Version extracted from FileVersion: {parsed.DisplayVersion}");
                    return parsed;
                }

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
                UpdaterLogger.LogError($"Failed to get FileVersionInfo for {exePath}", ex);
                return new VersionInfo { Version = "Unknown", Channel = "Unknown" };
            }
        }

        private static bool TryParseVersionFromString(string versionString, out VersionInfo versionInfo)
        {
            versionInfo = null;
            if (string.IsNullOrEmpty(versionString))
                return false;

            var parsed = VersionInfo.ParseFromExecutable(versionString);
            if (parsed.Channel != "Unknown")
            {
                versionInfo = parsed;
                return true;
            }
            return false;
        }

        private static VersionInfo TryGetAssemblyVersionInfo(string exePath, string fallbackVersion)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    UpdaterLogger.LogInfo($"Attempting to load assembly (attempt {attempt}/{maxRetries}): {exePath}");

                    if (attempt > 1)
                    {
                        System.Threading.Thread.Sleep(1000 * attempt);
                    }

                    var tempPath = Path.Combine(Path.GetTempPath(), $"BMC_temp_{Guid.NewGuid()}.exe");
                    File.Copy(exePath, tempPath, true);

                    try
                    {
                        var assembly = System.Reflection.Assembly.LoadFile(tempPath);

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

                        UpdaterLogger.LogInfo("Assembly loaded but no version information found in attributes");
                        return null;
                    }
                    finally
                    {
                        try
                        {
                            if (File.Exists(tempPath))
                                File.Delete(tempPath);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Assembly load attempt {attempt} failed: {ex.Message}");

                    if (attempt < maxRetries)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }
                }
            }

            UpdaterLogger.LogWarning($"Failed to load assembly after {maxRetries} attempts");
            return null;
        }
    }
}