namespace Updater.Utils
{
    public static class Constants
    {
        // Retry and timeout constants
        public const int DEFAULT_MAX_RETRIES = 3;
        public const int DEFAULT_RETRY_DELAY_MS = 1000;
        public const int VERSION_LOAD_BASE_DELAY_MS = 2000;
        public const int GITHUB_UPDATE_DELAY_MS = 2000;
        public const int PROCESS_WAIT_TIMEOUT_MS = 5000;
        public const int PROCESS_KILL_TIMEOUT_MS = 10000;
        public const int FILE_ACCESS_RETRY_DELAY_MS = 500;
        public const int FILE_SYSTEM_FLUSH_DELAY_MS = 100;
        public const int GITHUB_FILE_STABILIZATION_DELAY_MS = 3000;

        // Network file handling constants
        public const int NETWORK_FILE_MAX_ATTEMPTS = 8;
        public const int NETWORK_FILE_BASE_DELAY_MS = 400;
        public const int NETWORK_FILE_PROGRESSIVE_DELAY_MS = 100;

        // File operation constants
        public const int FILE_ACCESS_MAX_RETRIES = 3;
        public const int OPENFILES_TIMEOUT_SECONDS = 5;

        // Lock file and signal constants
        public const int BMC_LOCK_FILE_TIMEOUT_SECONDS = 30;
        public const int BMC_RESPONSE_FILE_TIMEOUT_SECONDS = 45;

        // Progress reporting constants
        public const int PROGRESS_UPDATE_INTERVAL_MS = 250;

        // File names and extensions
        public const string BMC_EXECUTABLE = "BMC.exe";
        public const string BACKUP_FILE_SUFFIX = ".backup.";
        public const string BMC_LOCK_FILE_PATTERN = "*.bmc-lock";
        public const string BMC_RESPONSE_FILE_PATTERN = ".bmc-response-*";

        // Process names
        public const string BMC_PROCESS_NAME = "BMC";
        public const string WEBPARAM_PROCESS_NAME = "WebParam";

        // Unknown values
        public const string UNKNOWN_VERSION = "Unknown";

        // Log directory
        public const string LOG_DIRECTORY = "Log";
        public const string UPDATER_LOG_FILE = "updaterLogs.txt";
        public const string DEBUG_LOG_FILE = "updater.log";
        public const string AUTO_UPDATE_LOG_FILE = "AutoUpdate.txt";

        // Configuration file
        public const string BMC_CONFIG_FILE = "BMC.ini";
        public const string GITHUB_CONFIG_FILE = "github.config";

        // GitHub API
        public const string GITHUB_API_URL = "https://api.github.com/repos/celarc/BMC/releases";
    }
}