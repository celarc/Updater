// Form1.cs - Refactored for C# 7.3 with GitHub Integration
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Updater.Services;
using Updater.Models;
using Updater.Utils;
using System.Diagnostics.Eventing.Reader;

namespace Updater
{
    public partial class Form1 : Form
    {
        private readonly UpdateManager _updateManager;
        private readonly CommandLineHandler _commandLineHandler;
        private GitHubRelease _selectedBetaRelease;
        private GitHubRelease _selectedStableRelease;
        private VersionInfo _currentVersion;

        public Form1()
        {
            InitializeComponent();
            _updateManager = new UpdateManager();
            _commandLineHandler = new CommandLineHandler();
            InitializeForm();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            await HandleCommandLineArguments();
            await LoadCurrentVersion();
            await GetLatestGitHubVersion(false);
            if(panel1.Visible)await GetLatestGitHubVersion(true);
        }

        private void InitializeForm()
        {
            //panel1.Visible = true;
        }

        private async Task HandleCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();

            if (args.Contains("updateBMC"))
            {
                await HandleAutomaticUpdate(UpdateType.BMC);
            }
            else if (args.Contains("updateWebParam"))
            {
                await HandleAutomaticUpdate(UpdateType.WebParam);
            }
            else if (args.Contains("updateBETA"))
            {
                panel1.Visible = true;
            }
        }

        private async Task HandleAutomaticUpdate(UpdateType updateType)
        {
            try
            {
                var logBuilder = new UpdateLogBuilder();
                logBuilder.StartUpdate();
                UpdaterLogger.LogInfo($"Starting automatic update for {updateType}");

                var progress = new Progress<UpdateProgress>(p => OnUpdateProgress(p, updateType));
                UpdateResult result;

                if (updateType == UpdateType.BMC)
                {
                    result = await _updateManager.UpdateBMCAsync(UpdateSource.FtpStable, progress, null);
                }
                else
                {
                    result = await _updateManager.UpdateWebParamAsync(progress);
                }

                logBuilder.CompleteUpdate(result);
                await _updateManager.WriteUpdateLogAsync(logBuilder.ToString(),
                    updateType.ToString());

                UpdaterLogger.LogInfo($"Automatic update for {updateType} completed. Success: {result.Success}");
                this.Close();
            }
            catch (Exception ex)
            {
                UpdaterLogger.LogError($"Automatic update for {updateType} failed", ex);
                this.Close();
            }
        }

        private async Task LoadCurrentVersion(bool isAfterGitHubUpdate = false)
        {
            const int maxRetries = 3;
            int baseDelay = isAfterGitHubUpdate ? 4000 : 2000; // Longer delay for GitHub updates

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Increase delay to ensure file operations are complete
                    // Use longer delays for GitHub updates as they may need more time
                    await Task.Delay(baseDelay * attempt);

                    UpdaterLogger.LogInfo($"Attempting to load current version (attempt {attempt}/{maxRetries}){(isAfterGitHubUpdate ? " after GitHub update" : "")}");
                    _currentVersion = await _updateManager.GetCurrentBMCVersionAsync();

                    if (_currentVersion != null && !string.IsNullOrEmpty(_currentVersion.Version) && _currentVersion.Version != "Unknown")
                    {
                        lCurrentVersion.Text = _currentVersion.DisplayVersion;
                        UpdaterLogger.LogInfo($"Current version loaded successfully: {_currentVersion.DisplayVersion}");
                        return; // Success, exit retry loop
                    }
                    else
                    {
                        UpdaterLogger.LogWarning($"Version load attempt {attempt} returned unknown/empty version");
                    }
                }
                catch (Exception ex)
                {
                    UpdaterLogger.LogWarning($"Version load attempt {attempt} failed: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        // Final attempt failed
                        UpdaterLogger.LogError($"Could not load current version after {maxRetries} attempts", ex);
                        lCurrentVersion.Text = "Neznana";
                        return;
                    }

                    // Wait before retrying - longer waits for GitHub updates
                    await Task.Delay(isAfterGitHubUpdate ? 2000 : 1000);
                }
            }
        }

        // BMC Stable Update
        private async void ButtonUpdateStable_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("BMC"))
            {
                MessageBox.Show(SlovenianMessages.CloseBMCPrograms);
                UpdaterLogger.LogWarning("BMC process running, stable update cancelled by user");
                return;
            }

            var downloadSource = _selectedStableRelease != null ? "iz internet" : "iz FTP";
            var message = $"Preveri in prenesi posodobitve za BMC {downloadSource}?";

            if (MessageBox.Show(message, SlovenianMessages.Confirm, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                await PerformUpdate(UpdateType.BMCStable);
            }
        }

        // BMC Beta Update
        private async void ButtonUpdateBeta_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("BMC"))
            {
                MessageBox.Show(SlovenianMessages.CloseBMCPrograms);
                UpdaterLogger.LogWarning("BMC process running, beta update cancelled by user");
                return;
            }

            await PerformUpdate(UpdateType.BMCBeta);
        }

        // WebParam Update
        private async void ButtonUpdateWebParam_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("WebParam"))
            {
                MessageBox.Show(SlovenianMessages.CloseWebParamPrograms);
                UpdaterLogger.LogWarning("WebParam process running, update cancelled by user");
                return;
            }

            var message = "Preveri in prenesi posodobitve za WebParam?";
            if (MessageBox.Show(message, SlovenianMessages.Confirm, MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                await PerformUpdate(UpdateType.WebParam);
            }
        }

        private async Task PerformUpdate(UpdateType updateType)
        {
            try
            {
                DisableUpdateButton(updateType, true);
                ResetProgress(updateType);

                var progress = new Progress<UpdateProgress>(p => OnUpdateProgress(p, updateType));
                var source = DetermineUpdateSource(updateType);

                UpdateResult result;
                if (updateType == UpdateType.WebParam)
                {
                    result = await _updateManager.UpdateWebParamAsync(progress);
                }
                else
                {
                    GitHubRelease selectedRelease = null;
                    if (source == UpdateSource.GitHub)
                    {
                        selectedRelease = (updateType == UpdateType.BMCBeta) ? _selectedBetaRelease : _selectedStableRelease;
                    }

                    result = await _updateManager.UpdateBMCAsync(source, progress, selectedRelease);
                }

                await ShowUpdateResult(result, updateType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{SlovenianMessages.UpdateFailed}: {ex.Message}");
                UpdaterLogger.LogError("Update failed in PerformUpdate", ex);
            }
            finally
            {
                DisableUpdateButton(updateType, false);
                ResetProgressFormat(updateType);
            }
        }

        private UpdateSource DetermineUpdateSource(UpdateType updateType)
        {
            if (updateType == UpdateType.BMCBeta && _selectedBetaRelease == null)
            {
                return UpdateSource.FtpBeta;
            }
            else if (updateType == UpdateType.BMCStable && _selectedStableRelease == null)
            {
                return UpdateSource.FtpStable;
            }
            else
            {
                return UpdateSource.GitHub;
            }
        }

        private void OnUpdateProgress(UpdateProgress progress, UpdateType updateType = UpdateType.BMC)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<UpdateProgress, UpdateType>(OnUpdateProgress), progress, updateType);
                return;
            }

            if (progress.IsError)
            {
                MessageBox.Show(progress.Exception?.ToString() ?? SlovenianMessages.UnknownError);
                UpdaterLogger.LogError("Update progress error", progress.Exception);
                return;
            }

            var progressBar = GetProgressBarForUpdateType(updateType);
            progressBar.EditValue = progress.PercentComplete;
            progressBar.Properties.ShowTitle = true;
            progressBar.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            progressBar.Properties.DisplayFormat.FormatString = "{0}%";
            if (progress.StatusMessage.Length > 0)
            {
                richTextBox1.AppendText(progress.StatusMessage + Environment.NewLine);
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            }

            //if (!string.IsNullOrEmpty(progress.StatusMessage))
            //{
            //    progressBar.Properties.DisplayFormat.FormatString = progress.StatusMessage;
            //}
        }

        private DevExpress.XtraEditors.ProgressBarControl GetProgressBarForUpdateType(UpdateType updateType)
        {
            switch (updateType)
            {
                case UpdateType.BMCBeta:
                    return progressBarControlBeta;
                case UpdateType.WebParam:
                    return progressBarControl2;
                default:
                    return progressBarControl1;
            }
        }

        private void DisableUpdateButton(UpdateType updateType, bool disable)
        {
            SimpleButton button;
            switch (updateType)
            {
                case UpdateType.BMCBeta:
                    button = downloadBetaBt;
                    break;
                case UpdateType.WebParam:
                    button = downloadWebparamBT;
                    break;
                default:
                    button = downloadStableBt;
                    break;
            }
            button.Enabled = !disable;
        }

        private void ResetProgress(UpdateType updateType)
        {
            var progressBar = GetProgressBarForUpdateType(updateType);
            progressBar.EditValue = 0;
            richTextBox1.Text = "";
        }

        private void ResetProgressFormat(UpdateType updateType)
        {
            var progressBar = GetProgressBarForUpdateType(updateType);
            progressBar.Properties.DisplayFormat.FormatString = "{0}%";
        }

        private async Task ShowUpdateResult(UpdateResult result, UpdateType updateType)
        {
            var isGitHubUpdate = (updateType == UpdateType.BMCBeta && _selectedBetaRelease != null) ||
                                (updateType == UpdateType.BMCStable && _selectedStableRelease != null);
            var downloadMethod = isGitHubUpdate ? "iz interneta" : "iz FTP";

            if (result.Success)
            {
                // Refresh the current version display immediately after successful update
                // Pass flag to indicate if this was a GitHub update for better timing
                await LoadCurrentVersion(isGitHubUpdate);

                // Force UI refresh
                lCurrentVersion.Refresh();
                Application.DoEvents();

                MessageBox.Show(string.Format(SlovenianMessages.DownloadSuccessful, downloadMethod));
                UpdaterLogger.LogInfo($"Download completed successfully using {downloadMethod}. New version: {lCurrentVersion.Text}");
            }
            else
            {
                MessageBox.Show($"{SlovenianMessages.UpdateFailed}: {result.Message}");
                UpdaterLogger.LogError($"Update failed: {result.Message}");
            }
        }

        // Version Selection Methods
        private async void ButtonSelectBetaVersion_Click(object sender, EventArgs e)
        {
            await SelectGitHubVersion(true);
        }

        private async void ButtonSelectStableVersion_Click(object sender, EventArgs e)
        {
            await SelectGitHubVersion(false);
        }
        private async Task GetLatestGitHubVersion(bool isBeta)
        {
            try
            {
                var updater = new GitHubUpdater();
                var allReleases = await updater.GetReleases();

                var filteredReleases = isBeta
                    ? allReleases.Where(r => r.Verzija.Contains("BETA")).ToList()
                    : allReleases.Where(r => r.Verzija.Contains("STABLE")).ToList();

                if(isBeta) lLatestBeta.Text = filteredReleases[0].Ime;
                else lLastVersion.Text = filteredReleases[0].Ime;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{SlovenianMessages.VersionError}: {ex.Message}\n\n{SlovenianMessages.WillUseFtpDownload}",
                    SlovenianMessages.VersionError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdaterLogger.LogError("Error loading GitHub versions for stable", ex);
            }
        }
        private async Task SelectGitHubVersion(bool isBeta)
        {
            try
            {
                var updater = new GitHubUpdater();
                var allReleases = await updater.GetReleases();

                var filteredReleases = isBeta
                    ? allReleases.Where(r => r.Verzija.Contains("BETA")).ToList()
                    : allReleases.Where(r => r.Verzija.Contains("STABLE")).ToList();

                using (var versionForm = new VerzijeGrid(filteredReleases, _currentVersion?.DisplayVersion))
                {
                    if (versionForm.ShowDialog() == DialogResult.OK && versionForm.SelectedRelease != null)
                    {
                        HandleVersionSelection(versionForm.SelectedRelease, isBeta);
                    }
                    else
                    {
                        ClearVersionSelection(isBeta);
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"{SlovenianMessages.VersionError}: {ex.Message}\n\n{SlovenianMessages.WillUseFtpDownload}",
                    SlovenianMessages.VersionError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdaterLogger.LogError("Error loading GitHub versions for beta", ex);
                ClearVersionSelection(isBeta);
            }
        }

        private void HandleVersionSelection(GitHubRelease selectedRelease, bool isBeta)
        {
            if (isBeta)
            {
                _selectedBetaRelease = selectedRelease;
                labelBeta.Text = selectedRelease.Verzija;
                downloadBetaBt.Text = $"Prenesi {selectedRelease.Verzija}";
            }
            else
            {
                _selectedStableRelease = selectedRelease;
                labelStable.Text = selectedRelease.Verzija;
                downloadStableBt.Text = $"Prenesi {selectedRelease.Verzija}";
            }

            var assetCount = selectedRelease.Assets?.Count ?? 0;
            XtraMessageBox.Show(string.Format(SlovenianMessages.SelectedVersion, selectedRelease.Verzija, assetCount),
                isBeta ? "Beta verzije" : "Stabilne verzije",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ClearVersionSelection(bool isBeta)
        {
            if (isBeta)
            {
                _selectedBetaRelease = null;
                labelBeta.Text = "BETA posodobitve";
                downloadBetaBt.Text = "Prenesi najnovejše posodobitve";
            }
            else
            {
                _selectedStableRelease = null;
                labelStable.Text = "STABLE posodobitve";
                downloadStableBt.Text = "Prenesi najnovejše posodobitve";
            }
        }

        private void ButtonClearBetaVersion_Click(object sender, EventArgs e)
        {
            ClearVersionSelection(true);
        }

        private void ButtonClearStableVersion_Click(object sender, EventArgs e)
        {
            ClearVersionSelection(false);
        }
    }

    public enum UpdateType
    {
        BMC,
        BMCStable,
        BMCBeta,
        WebParam
    }

    public class CommandLineHandler
    {
        public bool ShouldUpdateBMC => Environment.GetCommandLineArgs().Contains("updateBMC");
        public bool ShouldUpdateWebParam => Environment.GetCommandLineArgs().Contains("updateWebParam");
        public bool ShouldShowBeta => Environment.GetCommandLineArgs().Contains("updateBETA");
    }

    public class UpdateLogBuilder
    {
        private string _log = "";

        public void StartUpdate()
        {
            _log += $"Samodejna posodobitev začeta ob {DateTime.Now:dd.MM.yyyy HH:mm:ss}{Environment.NewLine}";
        }

        public void CompleteUpdate(UpdateResult result)
        {
            if (result.Success)
            {
                _log += $"Posodobitve uspešno prenesene, končano ob: {DateTime.Now:dd.MM.yyyy HH:mm:ss}{Environment.NewLine}{Environment.NewLine}";
            }
            else
            {
                _log += $"Napaka med prenosom: {result.Message}{Environment.NewLine}";
            }
        }

        public override string ToString() => _log;
    }
}