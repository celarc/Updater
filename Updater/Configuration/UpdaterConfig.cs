using System;
using System.IO;
using System.Xml;

namespace Updater.Configuration
{
    public class UpdaterConfig
    {
        private static UpdaterConfig _instance;
        private static readonly object _lock = new object();

        public string BMCPath { get; private set; } = @"E:\BMC\";
        public string WebParamPath { get; private set; } = @"C:\inetpub\WebParam\";
        public string FtpUrl { get; private set; } = "bmc.si";
        public string FtpUsername { get; private set; } = "updater@bmc.si";
        public string FtpPassword { get; private set; } = "fcc1b727289ac03db7e76f6291039923";
        public string GitHubPersonalAccessToken { get; private set; } = "";

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
                    BMCPath = bmcPathNode[0].InnerText;
                }

                var webParamPathNode = xmlDoc.GetElementsByTagName("POT_WEB_PARAM");
                if (webParamPathNode.Count > 0 && !string.IsNullOrEmpty(webParamPathNode[0].InnerText))
                {
                    WebParamPath = webParamPathNode[0].InnerText;
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