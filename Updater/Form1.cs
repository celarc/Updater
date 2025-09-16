// Form1.cs - Refactored for C# 7.3 with GitHub Integration
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Updater.Services;
using Updater.Models;
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

                this.Close();
            }
            catch (Exception)
            {
                this.Close();
            }
        }

        private async Task LoadCurrentVersion()
        {
            try
            {
                _currentVersion = await _updateManager.GetCurrentBMCVersionAsync();
                lCurrentVersion.Text = _currentVersion.DisplayVersion;
            }
            catch
            {
                lCurrentVersion.Text = "Neznana";
            }
        }

        // BMC Stable Update
        private async void ButtonUpdateStable_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("BMC"))
            {
                MessageBox.Show("Zapri vse BMC programe!");
                return;
            }

            var downloadSource = _selectedStableRelease != null ? "iz internet" : "iz FTP";
            var message = $"Preveri in prenesi posodobitve za BMC {downloadSource}?";

            if (MessageBox.Show(message, "Potrdi", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                await PerformUpdate(UpdateType.BMCStable);
            }
        }

        // BMC Beta Update
        private async void ButtonUpdateBeta_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("BMC"))
            {
                MessageBox.Show("Zapri vse BMC programe!");
                return;
            }

            await PerformUpdate(UpdateType.BMCBeta);
        }

        // WebParam Update
        private async void ButtonUpdateWebParam_Click(object sender, EventArgs e)
        {
            if (_updateManager.IsApplicationRunning("WebParam"))
            {
                MessageBox.Show("Zapri vse WebParam programe!");
                return;
            }

            var message = "Preveri in prenesi posodobitve za WebParam?";
            if (MessageBox.Show(message, "Potrdi", MessageBoxButtons.OKCancel) == DialogResult.OK)
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

                ShowUpdateResult(result, updateType);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Posodobitev ni uspela: {ex.Message}");
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
                MessageBox.Show(progress.Exception?.ToString() ?? "Neznana napaka");
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

        private async void ShowUpdateResult(UpdateResult result, UpdateType updateType)
        {
            var downloadMethod = (updateType == UpdateType.BMCBeta && _selectedBetaRelease != null) ||
                                (updateType == UpdateType.BMCStable && _selectedStableRelease != null)
                                ? "iz interneta" : "iz FTP";

            if (result.Success)
            {
                await LoadCurrentVersion();
                MessageBox.Show($"Prenos {downloadMethod} uspel!");
            }
            else
            {
                MessageBox.Show($"Posodobitev ni uspela: {result.Message}");
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
                XtraMessageBox.Show($"Error loading versions: {ex.Message}\n\nWill use FTP download.",
                    "Version Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                XtraMessageBox.Show($"Error loading versions: {ex.Message}\n\nWill use FTP download.",
                    "Version Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            XtraMessageBox.Show($"Izbrana verzija: {selectedRelease.Verzija}\nDatoteke za prenos: {assetCount}",
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
            _log += $"Automatic update started at {DateTime.Now:dd.MM.yyyy HH:mm:ss}{Environment.NewLine}";
        }

        public void CompleteUpdate(UpdateResult result)
        {
            if (result.Success)
            {
                _log += $"Updates successfully downloaded, completed at: {DateTime.Now:dd.MM.yyyy HH:mm:ss}{Environment.NewLine}{Environment.NewLine}";
            }
            else
            {
                _log += $"Error occurred during download: {result.Message}{Environment.NewLine}";
            }
        }

        public override string ToString() => _log;
    }
}