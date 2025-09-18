namespace Updater.Utils
{
    public static class SlovenianMessages
    {
        // Main UI Messages
        public const string CloseBMCPrograms = "Zapri vse BMC programe!";
        public const string CloseWebParamPrograms = "Zapri vse WebParam programe!";
        public const string Confirm = "Potrdi";
        public const string UpdateFailed = "Posodobitev ni uspela";
        public const string UnknownError = "Neznana napaka";
        public const string UpdateSuccessful = "Posodobitev uspešna";
        public const string DownloadSuccessful = "Prenos {0} uspel!"; // {0} = method (FTP/GitHub)
        public const string VersionError = "Napaka pri verzijah";
        public const string WillUseFtpDownload = "Uporabil bom FTP prenos.";
        public const string SelectedVersion = "Izbrana verzija: {0}\nDatoteke za prenos: {1}";
        public const string VersionAlreadyInstalled = "Ta verzija je že nameščena.";
        public const string Versions = "Verzije";
        public const string NoSelection = "Ni izbire";
        public const string PleaseSelectVersion = "Prosim izberite verzijo.";

        // Progress Messages
        public const string StartingGitHubDownload = "Začenjam GitHub prenos...";
        public const string StartingFtpDownload = "Začenjam FTP prenos...";
        public const string CountingFiles = "Štejem datoteke...";
        public const string Downloading = "Prenašam: {0}";
        public const string Downloaded = "Preneseno: {0}";
        public const string KillingProcess = "Zapiram proces: {0} (ID: {1})";
        public const string ProcessDidNotExit = "Proces {0} se ni zaprl v določenem času";
        public const string ProcessTerminatedSuccessfully = "Proces {0} uspešno zaprt";
        public const string FailedToKillProcess = "Napaka pri zapiranju procesa {0}: {1}";
        public const string WaitingForFileHandles = "Čakam na sproščanje datotek...";
        public const string FileStillInUse = "Datoteka je še vedno v uporabi po zaprtju procesov";
        public const string FileNowAvailable = "Datoteka je zdaj na voljo za zamenjavo";
        public const string ErrorKillingProcesses = "Napaka pri zapiranju procesov: {0}";
        public const string FileInUse = "Datoteka {0} je v uporabi ({1}), poskušam zapreti procese...";
        public const string AttemptingToRename = "Poskušam preimenovati obstoječo datoteko {0}...";
        public const string ExistingFileRenamed = "Obstoječa datoteka preimenovana v {0}, nadaljujem s prenosom...";
        public const string ProcessesKilledSuccessfully = "Procesi uspešno zaprti, nadaljujem s prenosom...";
        public const string CouldNotDownload = "Ne morem prenesti {0} - {1}. Poskus preimenovanja ni uspel: {2}";
        public const string DownloadFailed = "Prenos ni uspel za {0}: {1}. Ustavljam vse prenose.";

        // Update Results
        public const string BMCStillRunning = "BMC aplikacija še vedno teče in je ni mogoče ustaviti. Prosim zaprite BMC ročno pred posodobitvijo.";
        public const string BMCUpdateFailed = "BMC posodobitev ni uspela: {0}";
        public const string WebParamUpdateFailed = "WebParam posodobitev ni uspela: {0}";
        public const string GitHubDownloadFailed = "GitHub prenos ni uspel: {0}";
        public const string FtpDownloadFailed = "FTP prenos ni uspel: {0}";
        public const string NoAssetsFound = "V izbrani izdaji ni najdenih datotek";
        public const string GitHubUpdateCompleted = "GitHub posodobitev uspešno končana";
        public const string DownloadedFiles = "Preneseno {0} od {1} datotek";
        public const string DownloadedFilesCount = "Preneseno {0} datotek";
        public const string FinalizingTransfer = "Dokončujem prenos datotek...";

        // File operations
        public const string FileDoesNotExist = "Datoteka ne obstaja";
        public const string FileInUseByProcesses = "Datoteko uporabljajo BMC procesi: {0}";
        public const string FileAvailableForWriting = "Datoteka je na voljo za pisanje";
        public const string FileLockedByProcess = "Datoteko je zaklenil drug proces: {0}";
        public const string AccessDeniedToFile = "Dostop zavrnjen do datoteke: {0}";
        public const string CouldNotReplaceFile = "Ne morem zamenjati datoteke v uporabi: {0}";
        public const string RenamedLockedFile = "Preimenoval zaklenjena datoteka v rezervno: {0}";
        public const string CouldNotReplaceOrRename = "Ne morem zamenjati ali preimenovati datoteke v uporabi: {0}";
        public const string FailedToRenameLockedFile = "Napaka pri preimenovanju zaklenjene datoteke {0}: {1}";

        // Auto-update specific messages
        public const string AutoUpdateStarted = "Samodejna posodobitev začeta";
        public const string AutoUpdateCompleted = "Samodejna posodobitev končana";
        public const string AutoUpdateFailed = "Samodejna posodobitev ni uspela";
    }
}