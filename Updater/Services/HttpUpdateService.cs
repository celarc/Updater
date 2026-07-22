using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Updater.Configuration;
using Updater.Models;
using Updater.Utils;

namespace Updater.Services
{
    /// <summary>
    /// Posodabljanje prek HTTP endpointa update.php (privzeto https://bmc.si/servis/update.php).
    /// S strežnika dobi seznam datotek s SHA256 hashi in prenese SAMO tiste datoteke,
    /// katerih lokalni SHA256 se razlikuje od strežniškega (ali lokalno manjkajo).
    /// Uporablja HTTP Basic avtentikacijo s poverilnicami iz BMC.ini.
    /// Ob kakršnikoli napaki vrne CreateFailure (nikoli ne vrže izjeme navzven),
    /// kar v UpdateManager sproži samodejni preklop na stari FTP prenos.
    /// </summary>
    public class HttpUpdateService : IUpdateService
    {
        private const string TempFileSuffix = ".download";
        private const int DownloadBufferSize = 81920;

        private static readonly string[] ExcludedDirectories = { "app.publish", "node_modules", "obj", "bin" };

        private readonly UpdaterConfig _config;

        public HttpUpdateService()
        {
            _config = UpdaterConfig.Instance;
        }

        /// <summary>
        /// Glavna metoda posodobitve: 1) s strežnika prenese manifest (seznam datotek s SHA256),
        /// 2) primerja hashe z lokalnimi datotekami in sestavi seznam razlik,
        /// 3) prenese samo spremenjene/manjkajoče datoteke in vsako po prenosu še enkrat SHA256 preveri.
        /// Vrne CreateSuccess s številom prenesenih datotek ali CreateFailure ob napaki
        /// (napaka nikoli ne uide kot izjema - to omogoča FTP fallback v UpdateManager).
        /// </summary>
        public async Task<UpdateResult> UpdateAsync(UpdateSource source, string targetPath,
            IProgress<UpdateProgress> progress = null)
        {
            try
            {
                var channel = GetChannelPath(source);

                using (var client = CreateHttpClient())
                {
                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.FetchingManifest));
                    var manifest = await FetchManifestAsync(client, channel);

                    progress?.Report(UpdateProgress.Create(0, SlovenianMessages.VerifyingFiles));
                    CleanupOrphanedTempFiles(targetPath);
                    var filesToDownload = manifest.Where(e => ShouldUpdateFile(e, targetPath)).ToList();

                    UpdaterLogger.LogInfo($"HTTP update from channel {channel}: {manifest.Count} files in manifest, {filesToDownload.Count} need update");

                    if (filesToDownload.Count == 0)
                    {
                        progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FilesUpToDate));
                        return UpdateResult.CreateSuccess(string.Format(SlovenianMessages.DownloadedFilesCount, 0), 0);
                    }

                    var downloadedFiles = 0;
                    var failedFiles = new List<string>();

                    for (var i = 0; i < filesToDownload.Count; i++)
                    {
                        var entry = filesToDownload[i];
                        var startPercent = (int)((double)i / filesToDownload.Count * 100);
                        progress?.Report(UpdateProgress.Create(startPercent,
                            string.Format(SlovenianMessages.Downloading, entry.Path), entry.Path));

                        if (await DownloadFileAsync(client, channel, entry, targetPath, progress, i, filesToDownload.Count))
                        {
                            downloadedFiles++;
                            var endPercent = (int)((double)(i + 1) / filesToDownload.Count * 100);
                            progress?.Report(UpdateProgress.Create(endPercent,
                                string.Format(SlovenianMessages.Downloaded, entry.Path), entry.Path));
                        }
                        else
                        {
                            failedFiles.Add(entry.Path);
                        }
                    }

                    progress?.Report(UpdateProgress.Create(100, SlovenianMessages.FinalizingTransfer));

                    if (failedFiles.Count > 0)
                    {
                        var errorMsg = string.Format(SlovenianMessages.HttpDownloadFailed,
                            string.Join(", ", failedFiles));
                        UpdaterLogger.LogError(errorMsg);
                        return UpdateResult.CreateFailure(errorMsg);
                    }

                    var successMsg = string.Format(SlovenianMessages.DownloadedFilesCount, downloadedFiles);
                    UpdaterLogger.LogInfo(successMsg);
                    return UpdateResult.CreateSuccess(successMsg, downloadedFiles);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = string.Format(SlovenianMessages.HttpDownloadFailed, ex.Message);
                UpdaterLogger.LogError(errorMsg, ex);
                return UpdateResult.CreateFailure(errorMsg, ex);
            }
        }

        /// <summary>
        /// Ustvari HttpClient z glavo za HTTP Basic avtentikacijo (uporabnik/geslo iz BMC.ini:
        /// UPDATE_USERNAME/UPDATE_PASSWORD, če manjkata pa FTP_USERNAME/FTP_PASSWORD).
        /// Če uporabnika ni v configu, se zahteva pošlje brez avtentikacije.
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(100) };

            var username = _config.UpdateUsername;
            var password = _config.UpdatePassword;
            if (!string.IsNullOrEmpty(username))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }

            return client;
        }

        /// <summary>
        /// Preslika vir posodobitve v ime mape (kanala) na strežniku - zrcali FTP mape:
        /// HttpStable -> "STABLE", HttpBeta -> "BETA", HttpWebParam -> "6".
        /// </summary>
        private string GetChannelPath(UpdateSource source)
        {
            switch (source)
            {
                case UpdateSource.HttpStable:
                    return "STABLE";
                case UpdateSource.HttpBeta:
                    return "BETA";
                case UpdateSource.HttpWebParam:
                    return "6";
                default:
                    throw new ArgumentException($"Unsupported HTTP update source: {source}");
            }
        }

        /// <summary>
        /// S strežnika prenese manifest: GET ?action=list&amp;path={kanal} in razčleni JSON
        /// [{"path":"...","sha256":"...","size":...}, ...].
        /// To je EDINA metoda, ki pozna format odgovora strežnika - če lastnik spremeni
        /// ime akcije ali obliko JSON, se popravi samo tukaj.
        /// Vodilne poševnice v poteh odreže (strežnik lahko vrača "/BMC.exe" ali "BMC.exe").
        /// </summary>
        private async Task<List<UpdateManifestEntry>> FetchManifestAsync(HttpClient client, string channel)
        {
            var url = $"{_config.UpdateEndpointUrl}?action=list&path={Uri.EscapeDataString(channel)}";
            UpdaterLogger.LogInfo($"Fetching update manifest from {url}");

            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var manifestResponse = JsonConvert.DeserializeObject<UpdateManifestResponse>(json);
                var manifest = manifestResponse?.Files;

                if (manifest == null || manifest.Count == 0)
                    throw new InvalidOperationException("Update manifest is empty");

                foreach (var entry in manifest)
                {
                    if (!string.IsNullOrEmpty(entry.Path))
                        entry.Path = entry.Path.TrimStart('/', '\\');
                }

                return manifest;
            }
        }

        /// <summary>
        /// Odloči, ali je treba datoteko iz manifesta prenesti. Prenese se, če lokalna datoteka
        /// ne obstaja ALI se njen SHA256 razlikuje od strežniškega. Vedno preskoči:
        /// .fdb datoteke (Firebird baza se ne sme prepisati), nevarne poti (.. ali absolutne),
        /// ter izključene mape (app.publish, node_modules, obj, bin) - enako kot FTP prenos.
        /// Če lokalnega hasha ni mogoče izračunati (zaklenjena datoteka), se datoteka prenese.
        /// </summary>
        private bool ShouldUpdateFile(UpdateManifestEntry entry, string targetPath)
        {
            if (string.IsNullOrEmpty(entry?.Path) || string.IsNullOrEmpty(entry.Sha256))
                return false;

            if (!IsSafeRelativePath(entry.Path))
            {
                UpdaterLogger.LogWarning($"Skipping manifest entry with unsafe path: {entry.Path}");
                return false;
            }

            var extension = Path.GetExtension(entry.Path).ToLowerInvariant();
            if (extension == ".fdb")
                return false;

            var segments = entry.Path.Replace('\\', '/').Split('/');
            if (segments.Take(segments.Length - 1).Any(s =>
                s.StartsWith(".") || ExcludedDirectories.Contains(s.ToLowerInvariant())))
                return false;

            var localFilePath = GetLocalFilePath(entry, targetPath);
            if (!File.Exists(localFilePath))
                return true;

            var localHash = ComputeLocalSha256(localFilePath);
            if (localHash == null)
                return true;

            return !string.Equals(localHash, entry.Sha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Zaščita pred path-traversal napadom: zavrne absolutne poti in poti,
        /// ki vsebujejo ".." (manifest je vhod z oddaljenega strežnika, zato mu ne zaupamo slepo).
        /// </summary>
        private static bool IsSafeRelativePath(string path)
        {
            if (Path.IsPathRooted(path))
                return false;

            return !path.Replace('\\', '/').Split('/').Any(s => s == "..");
        }

        /// <summary>
        /// Sestavi lokalno pot datoteke: ciljna mapa + relativna pot iz manifesta
        /// (poševnice / pretvori v Windows \).
        /// </summary>
        private static string GetLocalFilePath(UpdateManifestEntry entry, string targetPath)
        {
            return Path.Combine(targetPath, entry.Path.Replace('/', '\\'));
        }

        /// <summary>
        /// Izračuna SHA256 lokalne datoteke (pretočno, brez branja cele datoteke v pomnilnik).
        /// Vrne null, če datoteke ni mogoče prebrati - klicatelj to obravnava kot "potrebuje posodobitev".
        /// </summary>
        private static string ComputeLocalSha256(string filePath)
        {
            try
            {
                using (var sha256 = SHA256.Create())
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    return ToHexString(sha256.ComputeHash(stream));
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not compute SHA256 for {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Pretvori bajte hasha v šestnajstiški zapis z malimi črkami (npr. "f976b3..."),
        /// enak format, kot ga vrača PHP-jev hash_file('sha256', ...).
        /// </summary>
        private static string ToHexString(byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Prenese eno datoteko in jo varno zamenja: prenos gre najprej v začasno datoteko
        /// (cilj + ".download"), po prenosu se SHA256 primerja s hashem iz manifesta.
        /// Ob neujemanju prenos 1x ponovi; če spet ne uspe, cilja NE prepiše in vrne false.
        /// Zaklenjene ciljne datoteke sprosti prek FileOperations.EnsureFileWritable
        /// (zapre BMC procese oz. naredi .backup), nato začasno datoteko premakne na cilj.
        /// </summary>
        private async Task<bool> DownloadFileAsync(HttpClient client, string channel,
            UpdateManifestEntry entry, string targetPath, IProgress<UpdateProgress> progress,
            int currentFileIndex, int totalFiles)
        {
            var targetFile = GetLocalFilePath(entry, targetPath);
            var tempFile = targetFile + TempFileSuffix;

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile));

                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    var downloadedHash = await DownloadToTempFileAsync(client, channel, entry, tempFile,
                        progress, currentFileIndex, totalFiles);

                    if (string.Equals(downloadedHash, entry.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (File.Exists(targetFile) && !FileOperations.EnsureFileWritable(targetFile))
                        {
                            File.Delete(tempFile);
                            return false;
                        }

                        if (File.Exists(targetFile))
                            File.Delete(targetFile);
                        File.Move(tempFile, targetFile);

                        UpdaterLogger.LogInfo($"Successfully downloaded and verified: {entry.Path}");
                        return true;
                    }

                    UpdaterLogger.LogWarning($"SHA256 mismatch for {entry.Path} (attempt {attempt}), expected {entry.Sha256}, got {downloadedHash}");
                    File.Delete(tempFile);
                }

                UpdaterLogger.LogError(string.Format(SlovenianMessages.HashMismatch, entry.Path));
                return false;
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Failed to download file: {entry.Path}", ex);
                try { if (File.Exists(tempFile)) File.Delete(tempFile); } catch { }
                return false;
            }
        }

        /// <summary>
        /// Pretočno prenese datoteko (GET ?action=file&amp;path={kanal}/{pot}) v začasno datoteko
        /// in MED prenosom sproti računa SHA256 (brez drugega branja z diska).
        /// Javlja napredek (odstotek, hitrost, preneseni bajti) največ vsakih 250 ms.
        /// Vrne SHA256 prenesene vsebine, ki ga klicatelj primerja z manifestom.
        /// </summary>
        private async Task<string> DownloadToTempFileAsync(HttpClient client, string channel,
            UpdateManifestEntry entry, string tempFile, IProgress<UpdateProgress> progress,
            int currentFileIndex, int totalFiles)
        {
            var remotePath = string.Join("/",
                (channel + "/" + entry.Path.Replace('\\', '/'))
                .Split('/')
                .Select(Uri.EscapeDataString));
            var url = $"{_config.UpdateEndpointUrl}?action=file&path={remotePath}";

            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? entry.Size;
                var lastProgressReport = DateTime.MinValue;
                var startTime = DateTime.Now;

                using (var sha256 = SHA256.Create())
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write,
                    FileShare.None, DownloadBufferSize, useAsync: true))
                {
                    var buffer = new byte[DownloadBufferSize];
                    long bytesTransferred = 0;
                    int bytesRead;

                    while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                        bytesTransferred += bytesRead;

                        if ((DateTime.Now - lastProgressReport).TotalMilliseconds >= Constants.PROGRESS_UPDATE_INTERVAL_MS)
                        {
                            lastProgressReport = DateTime.Now;
                            var elapsed = (DateTime.Now - startTime).TotalSeconds;
                            var speed = elapsed > 0 ? bytesTransferred / elapsed : 0;
                            var filePercent = totalBytes > 0 ? (double)bytesTransferred / totalBytes : 0;
                            var overallPercent = totalFiles > 0
                                ? (int)((currentFileIndex + filePercent) / totalFiles * 100)
                                : 0;

                            progress?.Report(UpdateProgress.CreateWithDetails(overallPercent,
                                string.Format(SlovenianMessages.Downloading, entry.Path), entry.Path,
                                bytesTransferred, totalBytes, speed, "HTTP"));
                        }
                    }

                    sha256.TransformFinalBlock(buffer, 0, 0);
                    return ToHexString(sha256.Hash);
                }
            }
        }

        /// <summary>
        /// Počisti osirotele *.download datoteke, ki bi ostale, če je bil prejšnji prenos
        /// prekinjen (izpad elektrike, ubit proces ipd.).
        /// </summary>
        private static void CleanupOrphanedTempFiles(string targetPath)
        {
            try
            {
                if (!Directory.Exists(targetPath))
                    return;

                foreach (var tempFile in Directory.GetFiles(targetPath, "*" + TempFileSuffix,
                    SearchOption.AllDirectories))
                {
                    try
                    {
                        File.Delete(tempFile);
                        UpdaterLogger.LogInfo($"Deleted orphaned temp file: {tempFile}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogWarning($"Could not clean up temp files in {targetPath}: {ex.Message}");
            }
        }

        /// <summary>Del vmesnika IUpdateService - preusmeri na skupni VersionDetector (enako kot FTP servis).</summary>
        public async Task<VersionInfo> GetCurrentVersionAsync(string applicationPath)
        {
            return await VersionDetector.GetCurrentVersionAsync(applicationPath);
        }

        /// <summary>Del vmesnika IUpdateService - preusmeri na skupni ProcessManager (enako kot FTP servis).</summary>
        public bool IsApplicationRunning(string processName)
        {
            return ProcessManager.IsApplicationRunning(processName);
        }

        /// <summary>Del vmesnika IUpdateService - preusmeri na skupni ProcessManager (enako kot FTP servis).</summary>
        public void StopRunningApplications(string processName)
        {
            ProcessManager.StopRunningApplications(processName);
        }
    }
}
