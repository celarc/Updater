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
        private string currentVersion;

        public VerzijeGrid(List<GitHubRelease> releases, string currentInstalledVersion = null)
        {
            InitializeComponent();
            currentVersion = currentInstalledVersion;
            SetupFormAppearance();
            SetupGridControl();
            PassVersionsToGrid(releases);
        }

        private void SetupFormAppearance()
        {
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

            gridViewReleases.Appearance.Row.Font = new Font("Segoe UI", 9.75f);
            gridViewReleases.Appearance.HeaderPanel.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            gridViewReleases.Appearance.FooterPanel.Font = new Font("Segoe UI", 9.75f);
            gridViewReleases.Appearance.Row.Options.UseFont = true;
            gridViewReleases.Appearance.HeaderPanel.Options.UseFont = true;

            gridViewReleases.OptionsView.EnableAppearanceEvenRow = true;
            gridViewReleases.OptionsView.EnableAppearanceOddRow = true;
            gridViewReleases.Appearance.OddRow.BackColor = Color.FromArgb(244, 244, 244);
            gridViewReleases.Appearance.EvenRow.BackColor = Color.White;

            gridViewReleases.OptionsSelection.EnableAppearanceFocusedCell = false;
            gridViewReleases.FocusRectStyle = DrawFocusRectStyle.RowFocus;

            gridViewReleases.RowCellStyle += GridViewReleases_RowCellStyle;
            gridViewReleases.SelectionChanged += GridViewReleases_SelectionChanged;
            gridViewReleases.FocusedRowChanged += GridViewReleases_FocusedRowChanged;
        }

        private void GridViewReleases_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            var gridView = sender as GridView;
            var release = gridView.GetRow(e.RowHandle) as GitHubRelease;

            if (release != null && IsCurrentVersion(release.Verzija))
            {
                e.Appearance.BackColor = Color.FromArgb(220, 220, 220);
                e.Appearance.ForeColor = Color.Gray;
                e.Appearance.Font = new Font(e.Appearance.Font, FontStyle.Italic);
            }
        }

        private void GridViewReleases_SelectionChanged(object sender, DevExpress.Data.SelectionChangedEventArgs e)
        {
            var gridView = sender as GridView;

            var release = gridView.GetRow(e.ControllerRow) as GitHubRelease;
            if (release != null && IsCurrentVersion(release.Verzija))
            {
                //gridView.UnselectRow(e.ControllerRow);
            }
        }

        private void GridViewReleases_FocusedRowChanged(object sender, DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            var gridView = sender as GridView;
            if (e.FocusedRowHandle >= 0)
            {
                var release = gridView.GetRow(e.FocusedRowHandle) as GitHubRelease;
                if (release != null && IsCurrentVersion(release.Verzija))
                {
                    simpleButtonSelect.Enabled = false;
                    //int nextRow = FindNextSelectableRow(gridView, e.FocusedRowHandle);
                    //if (nextRow >= 0)
                    //{
                    //    gridView.FocusedRowHandle = nextRow;
                    //}
                }
                else simpleButtonSelect.Enabled = true;
            }
        }

        private int FindNextSelectableRow(GridView gridView, int currentRow)
        {
            for (int i = currentRow + 1; i < gridView.RowCount; i++)
            {
                var release = gridView.GetRow(i) as GitHubRelease;
                if (release != null && !IsCurrentVersion(release.Verzija))
                {
                    return i;
                }
            }

            for (int i = currentRow - 1; i >= 0; i--)
            {
                var release = gridView.GetRow(i) as GitHubRelease;
                if (release != null && !IsCurrentVersion(release.Verzija))
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsCurrentVersion(string releaseVersion)
        {
            if (string.IsNullOrEmpty(currentVersion) || string.IsNullOrEmpty(releaseVersion))
                return false;

            return string.Equals(currentVersion, releaseVersion, StringComparison.OrdinalIgnoreCase);
        }

        public void PassVersionsToGrid(List<GitHubRelease> releases)
        {
            foreach (var release in releases)
            {
                if (IsCurrentVersion(release.Verzija))
                {
                    release.Opomba = "Trenutna verzija";
                }
            }

            gridControlReleases.DataSource = releases;
            ConfigureGridColumns();
        }

        private void ConfigureGridColumns()
        {
            gridViewReleases.Columns.Clear();

            var versionCol = gridViewReleases.Columns.AddVisible("Verzija", "Verzija");
            versionCol.OptionsColumn.AllowEdit = false;
            versionCol.Width = 120;

            var dateCol = gridViewReleases.Columns.AddVisible("Objavljeno", "Objavljeno");
            dateCol.DisplayFormat.FormatString = "yyyy-MM-dd";
            dateCol.OptionsColumn.AllowEdit = false;
            dateCol.Width = 120;

            var notesCol = gridViewReleases.Columns.AddVisible("Opomba", "Opomba");
            notesCol.OptionsColumn.AllowEdit = false;
            notesCol.OptionsColumn.AllowEdit = false;
            notesCol.OptionsColumn.AllowFocus = false;
            notesCol.OptionsColumn.AllowMerge = DevExpress.Utils.DefaultBoolean.False;
            notesCol.OptionsColumn.AllowSize = true;
            notesCol.OptionsColumn.AllowMove = false;

            var detailsCol = gridViewReleases.Columns.AddVisible("Opis", "Opis");
            detailsCol.OptionsColumn.AllowEdit = false;
            detailsCol.OptionsColumn.AllowMerge = DevExpress.Utils.DefaultBoolean.False;
            detailsCol.OptionsColumn.AllowSize = true;
            detailsCol.OptionsColumn.AllowMove = false;

            gridViewReleases.OptionsView.ShowGroupPanel = false;
            gridViewReleases.OptionsView.ShowAutoFilterRow = true;
            gridViewReleases.OptionsView.ShowIndicator = false;
            gridViewReleases.OptionsBehavior.Editable = false;
            gridViewReleases.OptionsSelection.MultiSelect = false;
            gridViewReleases.OptionsSelection.EnableAppearanceFocusedRow = true;
            gridViewReleases.OptionsDetail.EnableMasterViewMode = false;

            gridViewReleases.OptionsView.RowAutoHeight = true;
            gridViewReleases.OptionsView.ColumnAutoWidth = false;

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
                var release = gridViewReleases.GetFocusedRow() as GitHubRelease;
                if (release != null)
                {
                    if (IsCurrentVersion(release.Verzija))
                    {
                        XtraMessageBox.Show("Ta verzija je že nameščena.", "Verzije",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    SelectedRelease = release;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            else
            {
                XtraMessageBox.Show("Prosim izberite verzijo.", "Ni izbire",
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