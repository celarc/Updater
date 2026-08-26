using System.Collections.Generic;
using Newtonsoft.Json;

namespace Updater.Models
{
    public class UpdateManifestEntry
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("sha256")]
        public string Sha256 { get; set; }

        [JsonProperty("size")]
        public long Size { get; set; }
    }

    public class UpdateManifestResponse
    {
        [JsonProperty("files")]
        public List<UpdateManifestEntry> Files { get; set; }
    }
}
