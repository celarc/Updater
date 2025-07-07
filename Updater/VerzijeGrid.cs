using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static DevExpress.Utils.Svg.CommonSvgImages;

namespace Updater
{
    public partial class VerzijeGrid : XtraForm
    {
        public GitHubRelease SelectedRelease { get; private set; }

        public VerzijeGrid(List<GitHubRelease> releases)
        {
            InitializeComponent();
            SetupFormAppearance();
            SetupGridControl();
            PassVersionsToGrid(releases);
        }

        private void SetupFormAppearance()
        {
            // Form styling
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Appearance.BackColor = Color.White;
            this.Appearance.Options.UseBackColor = true;
        }

        private void SetupGridControl()
        {
            gridControlReleases.Font = new Font("Segoe UI", 9.75f);

            // GridView styling
            gridViewReleases.Appearance.Row.Font = new Font("Segoe UI", 9.75f);
            gridViewReleases.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            gridViewReleases.Appearance.FooterPanel.Font = new Font("Segoe UI", 9.75f);
            gridViewReleases.Appearance.Row.Options.UseFont = true;
            gridViewReleases.Appearance.HeaderPanel.Options.UseFont = true;

            // Enable alternating row colors
            gridViewReleases.OptionsView.EnableAppearanceEvenRow = true;
            gridViewReleases.OptionsView.EnableAppearanceOddRow = true;
            gridViewReleases.Appearance.OddRow.BackColor = Color.FromArgb(244, 244, 244);
            gridViewReleases.Appearance.EvenRow.BackColor = Color.White;

            // Remove focus rectangle
            gridViewReleases.OptionsSelection.EnableAppearanceFocusedCell = false;
            gridViewReleases.FocusRectStyle = DrawFocusRectStyle.RowFocus;
        }

        public void PassVersionsToGrid(List<GitHubRelease> releases)
        {
            gridControlReleases.DataSource = releases;
            ConfigureGridColumns();
        }

        private void ConfigureGridColumns()
        {
            gridViewReleases.Columns.Clear();

            // Version column
            var versionCol = gridViewReleases.Columns.AddVisible("Verzija", "Verzija");
            versionCol.OptionsColumn.AllowEdit = false;
            versionCol.Width = 120;

            // Date column
            var dateCol = gridViewReleases.Columns.AddVisible("Objavljeno", "Objavljeno");
            dateCol.DisplayFormat.FormatString = "yyyy-MM-dd";
            dateCol.OptionsColumn.AllowEdit = false;
            dateCol.Width = 120;

            // Pre-release column
            var prereleaseCol = gridViewReleases.Columns.AddVisible("Alpha", "Alpha");
            prereleaseCol.OptionsColumn.AllowEdit = false;
            prereleaseCol.Width = 80;

            // Notes column
            var notesCol = gridViewReleases.Columns.AddVisible("Opomba", "Opomba");
            notesCol.OptionsColumn.AllowEdit = false;
            notesCol.OptionsColumn.AllowEdit = false;
            notesCol.OptionsColumn.AllowFocus = false;
            notesCol.OptionsColumn.AllowMerge = DevExpress.Utils.DefaultBoolean.False;
            notesCol.OptionsColumn.AllowSize = true;
            notesCol.OptionsColumn.AllowMove = false;

            // Grid behavior options
            gridViewReleases.OptionsView.ShowGroupPanel = false;
            gridViewReleases.OptionsView.ShowAutoFilterRow = true;
            gridViewReleases.OptionsView.ShowIndicator = false;
            gridViewReleases.OptionsBehavior.Editable = false;
            gridViewReleases.OptionsSelection.MultiSelect = false;
            gridViewReleases.OptionsSelection.EnableAppearanceFocusedRow = true;
            gridViewReleases.OptionsDetail.EnableMasterViewMode = false;

            // Enable row auto-height for notes
            gridViewReleases.OptionsView.RowAutoHeight = true;
            gridViewReleases.OptionsView.ColumnAutoWidth = false;

            // Best fit columns after all settings
            gridViewReleases.BestFitColumns();
        }

        private void gridViewReleases_DoubleClick(object sender, EventArgs e)
        {
            SelectReleaseAndClose();
        }

        private void simpleButtonSelect_Click(object sender, EventArgs e)
        {
            SelectReleaseAndClose();
        }

        private void SelectReleaseAndClose()
        {
            if (gridViewReleases.FocusedRowHandle >= 0)
            {
                SelectedRelease = gridViewReleases.GetFocusedRow() as GitHubRelease;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                XtraMessageBox.Show("Please select a release first.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void simpleButtonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void VerzijeGrid_Load(object sender, EventArgs e)
        {

        }
    }
}