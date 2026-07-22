using Newtonsoft.Json;

namespace Updater.Models
{
    /// <summary>
    /// Ena vrstica manifesta, ki ga vrne update.php (action=list):
    /// relativna pot datoteke na strežniku, njen SHA256 hash in velikost v bajtih.
    /// JsonProperty atributi se morajo ujemati z imeni polj v JSON odgovoru strežnika.
    /// </summary>
    public class UpdateManifestEntry
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }
}
