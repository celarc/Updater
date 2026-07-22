using System;
using System.IO;
using System.Xml;
using Updater.Utils;

namespace Updater.Configuration
{
    public class UpdaterConfig
    {
        private static UpdaterConfig _instance;
        private static readonly object _lock = new object();

        public string BMCPath { get; private set; } = @"E:\BMC\";
        public string WebParamPath { get; private set; } = @"C:\inetpub\WebParam\";
        public string FtpUrl { get; private set; } = "bmc.si";
        public string FtpUsername { get; private set; }
        public string FtpPassword { get; private set; }
        /// <summary>Naslov HTTP update endpointa (privzeto https://bmc.si/servis/update.php); v BMC.ini ga prepiše vozlišče UPDATE_URL.</summary>
        public string UpdateEndpointUrl { get; private set; } = Constants.UPDATE_ENDPOINT_URL;
        private string _updateUsername;
        private string _updatePassword;
        /// <summary>Uporabnik za Basic auth na update.php (UPDATE_USERNAME iz BMC.ini); če ni nastavljen, se uporabi FTP_USERNAME.</summary>
        public string UpdateUsername => !string.IsNullOrEmpty(_updateUsername) ? _updateUsername : FtpUsername;
        /// <summary>Geslo za Basic auth na update.php (UPDATE_PASSWORD iz BMC.ini); če ni nastavljeno, se uporabi FTP_PASSWORD.</summary>
        public string UpdatePassword => !string.IsNullOrEmpty(_updatePassword) ? _updatePassword : FtpPassword;
        /// <summary>Varovalka: UPDATE_DISABLE_HTTP=1 (ali true) v BMC.ini izklopi HTTP prenos in vsili stari FTP - brez ponovnega builda.</summary>
        public bool DisableHttpUpdate { get; private set; }
        public string GitHubPersonalAccessToken { get; private set; } = "11A7BA7EQ0aXB3OGiwCLUz_Jr0k6ezyRjfweNzFwvfHhL97S2r7M0kyffpLdjbT8jZ32V54J5QKV94Vb3U";

        public int NetworkFileRetryAttempts { get; private set; } = 10;
        public int NetworkFileRetryDelayMs { get; private set; } = 1000;
        public int NetworkFileSignalWaitAttempts { get; private set; } = 5;
        public int NetworkFileSignalWaitDelayMs { get; private set; } = 2000;

        public static UpdaterConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new UpdaterConfig();
                    }
                }
                return _instance;
            }
        }

        private UpdaterConfig()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                var appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var githubConfigPath = Path.Combine(appDirectory, "github.config");

                if (File.Exists(githubConfigPath))
                {
                    var pat = File.ReadAllText(githubConfigPath).Trim();
                    if (!string.IsNullOrEmpty(pat))
                    {
                        GitHubPersonalAccessToken = pat;
                    }
                }
            }
            catch (Exception ex)
            {
            }

            try
            {
                if (!File.Exists("BMC.ini")) return;

                var xmlDoc = new XmlDocument();
                using (var fs = new FileStream("BMC.ini", FileMode.Open, FileAccess.Read))
                {
                    xmlDoc.Load(fs);
                }

                var bmcPathNode = xmlDoc.GetElementsByTagName("POT_BMC");
                if (bmcPathNode.Count > 0 && !string.IsNullOrEmpty(bmcPathNode[0].InnerText))
                {
                    var configPath = bmcPathNode[0].InnerText.Trim();
                    BMCPath = configPath;

                }

                var webParamPathNode = xmlDoc.GetElementsByTagName("POT_WEB_PARAM");
                if (webParamPathNode.Count > 0 && !string.IsNullOrEmpty(webParamPathNode[0].InnerText))
                {
                    WebParamPath = webParamPathNode[0].InnerText;
                }

                var ftpUsernameNode = xmlDoc.GetElementsByTagName("FTP_USERNAME");
                if (ftpUsernameNode.Count > 0 && !string.IsNullOrEmpty(ftpUsernameNode[0].InnerText))
                {
                    FtpUsername = ftpUsernameNode[0].InnerText.Trim();
                }

                var ftpPasswordNode = xmlDoc.GetElementsByTagName("FTP_PASSWORD");
                if (ftpPasswordNode.Count > 0 && !string.IsNullOrEmpty(ftpPasswordNode[0].InnerText))
                {
                    FtpPassword = ftpPasswordNode[0].InnerText.Trim();
                }

                var updateUrlNode = xmlDoc.GetElementsByTagName(Constants.UPDATE_URL_NODE);
                if (updateUrlNode.Count > 0 && !string.IsNullOrEmpty(updateUrlNode[0].InnerText))
                {
                    UpdateEndpointUrl = updateUrlNode[0].InnerText.Trim();
                }

                var updateUsernameNode = xmlDoc.GetElementsByTagName(Constants.UPDATE_USERNAME_NODE);
                if (updateUsernameNode.Count > 0 && !string.IsNullOrEmpty(updateUsernameNode[0].InnerText))
                {
                    _updateUsername = updateUsernameNode[0].InnerText.Trim();
                }

                var updatePasswordNode = xmlDoc.GetElementsByTagName(Constants.UPDATE_PASSWORD_NODE);
                if (updatePasswordNode.Count > 0 && !string.IsNullOrEmpty(updatePasswordNode[0].InnerText))
                {
                    _updatePassword = updatePasswordNode[0].InnerText.Trim();
                }

                var disableHttpNode = xmlDoc.GetElementsByTagName(Constants.UPDATE_DISABLE_HTTP_NODE);
                if (disableHttpNode.Count > 0 && !string.IsNullOrEmpty(disableHttpNode[0].InnerText))
                {
                    var value = disableHttpNode[0].InnerText.Trim();
                    DisableHttpUpdate = value == "1" ||
                        value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }

                var networkRetryAttemptsNode = xmlDoc.GetElementsByTagName("NETWORK_RETRY_ATTEMPTS");
                if (networkRetryAttemptsNode.Count > 0 && int.TryParse(networkRetryAttemptsNode[0].InnerText, out var retryAttempts))
                {
                    NetworkFileRetryAttempts = retryAttempts;
                }

                var networkRetryDelayNode = xmlDoc.GetElementsByTagName("NETWORK_RETRY_DELAY_MS");
                if (networkRetryDelayNode.Count > 0 && int.TryParse(networkRetryDelayNode[0].InnerText, out var retryDelay))
                {
                    NetworkFileRetryDelayMs = retryDelay;
                }

                var networkSignalAttemptsNode = xmlDoc.GetElementsByTagName("NETWORK_SIGNAL_WAIT_ATTEMPTS");
                if (networkSignalAttemptsNode.Count > 0 && int.TryParse(networkSignalAttemptsNode[0].InnerText, out var signalAttempts))
                {
                    NetworkFileSignalWaitAttempts = signalAttempts;
                }

                var networkSignalDelayNode = xmlDoc.GetElementsByTagName("NETWORK_SIGNAL_WAIT_DELAY_MS");
                if (networkSignalDelayNode.Count > 0 && int.TryParse(networkSignalDelayNode[0].InnerText, out var signalDelay))
                {
                    NetworkFileSignalWaitDelayMs = signalDelay;
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}