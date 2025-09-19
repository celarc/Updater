namespace Updater.Models
{
    public class VersionInfo
    {
        public string Version { get; set; }
        public string Channel { get; set; }
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

        public static VersionInfo ParseFromExecutable(string versionString)
        {
            if (string.IsNullOrEmpty(versionString))
                return new VersionInfo { Version = "Unknown", Channel = "Unknown" };

            var upperVersion = versionString.ToUpperInvariant();

            if (upperVersion.Contains("BETA"))
            {
                var betaIndex = upperVersion.IndexOf("BETA");
                if (betaIndex >= 0)
                {
                    var afterBeta = versionString.Substring(betaIndex + 4);
                    if (afterBeta.StartsWith("-"))
                        afterBeta = afterBeta.Substring(1);

                    var cleanVersion = ExtractVersionNumber(afterBeta.Length > 0 ? afterBeta : versionString);

                    return new VersionInfo
                    {
                        Channel = "BETA",
                        Version = cleanVersion
                    };
                }
            }

            if (upperVersion.Contains("STABLE"))
            {
                var stableIndex = upperVersion.IndexOf("STABLE");
                if (stableIndex >= 0)
                {
                    var afterStable = versionString.Substring(stableIndex + 6);
                    if (afterStable.StartsWith("-"))
                        afterStable = afterStable.Substring(1);

                    var cleanVersion = ExtractVersionNumber(afterStable.Length > 0 ? afterStable : versionString);

                    return new VersionInfo
                    {
                        Channel = "STABLE",
                        Version = cleanVersion
                    };
                }
            }

            var dashIndex = versionString.IndexOf('-');
            if (dashIndex > 0)
            {
                var channel = versionString.Substring(0, dashIndex).ToUpperInvariant();
                var version = versionString.Substring(dashIndex + 1);

                if (channel == "BETA" || channel == "STABLE")
                {
                    return new VersionInfo
                    {
                        Channel = channel,
                        Version = ExtractVersionNumber(version)
                    };
                }
            }

            return new VersionInfo
            {
                Version = ExtractVersionNumber(versionString),
                Channel = "Unknown"
            };
        }

        private static string ExtractVersionNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "Unknown";

            var versionRegex = new System.Text.RegularExpressions.Regex(@"\d+(\.\d+)*");
            var match = versionRegex.Match(input);

            if (match.Success)
                return match.Value;

            return input.Trim();
        }
    }
}