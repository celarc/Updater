namespace Updater.Models
{
    public class VersionInfo
    {
        public string Version { get; set; }
        public string Channel { get; set; } // BETA, STABLE
        public bool IsInstalled { get; set; }
        public System.DateTime? ReleaseDate { get; set; }

        public string DisplayVersion => $"{Channel}-{Version}";

        public static VersionInfo Parse(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
                return new VersionInfo { Version = "Unknown", Channel = "Unknown" };

            var dashIndex = versionString.IndexOf('-');
            if (dashIndex > 0)
            {
                return new VersionInfo
                {
                    Channel = versionString.Substring(0, dashIndex),
                    Version = versionString.Substring(dashIndex + 1)
                };
            }

            return new VersionInfo { Version = versionString, Channel = "Unknown" };
        }
    }
}