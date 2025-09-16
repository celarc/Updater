namespace Updater
{
    partial class VerzijeGrid
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(VerzijeGrid));
            this.gridControlReleases = new DevExpress.XtraGrid.GridControl();
            this.gridViewReleases = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.simpleButtonSelect = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButtonCancel = new DevExpress.XtraEditors.SimpleButton();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlReleases)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewReleases)).BeginInit();
            this.SuspendLayout();
            // 
            // gridControlReleases
            // 
            this.gridControlReleases.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gridControlReleases.Location = new System.Drawing.Point(-1, 12);
            this.gridControlReleases.MainView = this.gridViewReleases;
            this.gridControlReleases.Name = "gridControlReleases";
            this.gridControlReleases.Size = new System.Drawing.Size(788, 387);
            this.gridControlReleases.TabIndex = 0;
            this.gridControlReleases.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewReleases});
            // 
            // gridViewReleases
            // 
            this.gridViewReleases.GridControl = this.gridControlReleases;
            this.gridViewReleases.Name = "gridViewReleases";
            // 
            // simpleButtonSelect
            // 
            this.simpleButtonSelect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.simpleButtonSelect.Location = new System.Drawing.Point(619, 440);
            this.simpleButtonSelect.Name = "simpleButtonSelect";
            this.simpleButtonSelect.Size = new System.Drawing.Size(75, 23);
            this.simpleButtonSelect.TabIndex = 1;
            this.simpleButtonSelect.Text = "Izberi verzijo";
            this.simpleButtonSelect.Click += new System.EventHandler(this.simpleButtonSelect_Click);
            // 
            // simpleButtonCancel
            // 
            this.simpleButtonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.simpleButtonCancel.Location = new System.Drawing.Point(700, 440);
            this.simpleButtonCancel.Name = "simpleButtonCancel";
            this.simpleButtonCancel.Size = new System.Drawing.Size(75, 23);
            this.simpleButtonCancel.TabIndex = 2;
            this.simpleButtonCancel.Text = "Prekliči";
            this.simpleButtonCancel.Click += new System.EventHandler(this.simpleButtonCancel_Click);
            // 
            // VerzijeGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(787, 475);
            this.Controls.Add(this.simpleButtonCancel);
            this.Controls.Add(this.simpleButtonSelect);
            this.Controls.Add(this.gridControlReleases);
            this.IconOptions.Icon = ((System.Drawing.Icon)(resources.GetObject("VerzijeGrid.IconOptions.Icon")));
            this.Name = "VerzijeGrid";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Verzije";
            this.Load += new System.EventHandler(this.VerzijeGrid_Load);
            ((System.ComponentModel.ISupportInitialize)(this.gridControlReleases)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewReleases)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraGrid.GridControl gridControlReleases;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewReleases;
        private DevExpress.XtraEditors.SimpleButton simpleButtonSelect;
        private DevExpress.XtraEditors.SimpleButton simpleButtonCancel;
    }
}