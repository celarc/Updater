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
            // Load GitHub PAT from github.config
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
                // GitHub config loading failed, PAT remains empty
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
            }
            catch (Exception ex)
            {
                // Configuration loading failed, using defaults
            }
        }
    }
}