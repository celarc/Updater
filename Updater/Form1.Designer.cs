namespace Updater
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.downloadStableBt = new DevExpress.XtraEditors.SimpleButton();
            this.labelStableBMC = new System.Windows.Forms.Label();
            this.progressBarControl1 = new DevExpress.XtraEditors.ProgressBarControl();
            this.downloadWebparamBT = new DevExpress.XtraEditors.SimpleButton();
            this.progressBarControl2 = new DevExpress.XtraEditors.ProgressBarControl();
            this.label3 = new System.Windows.Forms.Label();
            this.progressBarControlBeta = new DevExpress.XtraEditors.ProgressBarControl();
            this.labelBeta = new System.Windows.Forms.Label();
            this.downloadBetaBt = new DevExpress.XtraEditors.SimpleButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.simpleButtonBetaCleanVersion = new DevExpress.XtraEditors.SimpleButton();
            this.selectBetaVersionBt = new DevExpress.XtraEditors.SimpleButton();
            this.labelBetaBMC = new System.Windows.Forms.Label();
            this.labelCurrentVersion = new System.Windows.Forms.Label();
            this.selectStableVersionBt = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButtonStableCleanVersion = new DevExpress.XtraEditors.SimpleButton();
            this.labelStable = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControlBeta.Properties)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // downloadStableBt
            // 
            this.downloadStableBt.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton1.ImageOptions.Image")));
            this.downloadStableBt.Location = new System.Drawing.Point(400, 37);
            this.downloadStableBt.Name = "downloadStableBt";
            this.downloadStableBt.Size = new System.Drawing.Size(186, 27);
            this.downloadStableBt.TabIndex = 0;
            this.downloadStableBt.Text = "Preveri posodobitve";
            this.downloadStableBt.Click += new System.EventHandler(this.ButtonUpdateStable_Click);
            // 
            // labelStableBMC
            // 
            this.labelStableBMC.AutoSize = true;
            this.labelStableBMC.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStableBMC.Location = new System.Drawing.Point(12, 40);
            this.labelStableBMC.Name = "labelStableBMC";
            this.labelStableBMC.Size = new System.Drawing.Size(42, 18);
            this.labelStableBMC.TabIndex = 3;
            this.labelStableBMC.Text = "BMC";
            // 
            // progressBarControl1
            // 
            this.progressBarControl1.Location = new System.Drawing.Point(106, 37);
            this.progressBarControl1.Name = "progressBarControl1";
            this.progressBarControl1.Properties.EndColor = System.Drawing.Color.Lime;
            this.progressBarControl1.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControl1.Properties.StartColor = System.Drawing.Color.Green;
            this.progressBarControl1.Size = new System.Drawing.Size(288, 27);
            this.progressBarControl1.TabIndex = 4;
            // 
            // downloadWebparamBT
            // 
            this.downloadWebparamBT.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton2.ImageOptions.Image")));
            this.downloadWebparamBT.Location = new System.Drawing.Point(400, 149);
            this.downloadWebparamBT.Name = "downloadWebparamBT";
            this.downloadWebparamBT.Size = new System.Drawing.Size(186, 27);
            this.downloadWebparamBT.TabIndex = 5;
            this.downloadWebparamBT.Text = "Preveri posodobitve";
            this.downloadWebparamBT.Click += new System.EventHandler(this.ButtonUpdateWebParam_Click);
            // 
            // progressBarControl2
            // 
            this.progressBarControl2.Location = new System.Drawing.Point(106, 149);
            this.progressBarControl2.Name = "progressBarControl2";
            this.progressBarControl2.Properties.EndColor = System.Drawing.Color.Lime;
            this.progressBarControl2.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControl2.Properties.StartColor = System.Drawing.Color.Green;
            this.progressBarControl2.Size = new System.Drawing.Size(288, 27);
            this.progressBarControl2.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label3.Location = new System.Drawing.Point(12, 152);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 18);
            this.label3.TabIndex = 7;
            this.label3.Text = "Web param";
            // 
            // progressBarControlBeta
            // 
            this.progressBarControlBeta.Location = new System.Drawing.Point(74, 48);
            this.progressBarControlBeta.Name = "progressBarControlBeta";
            this.progressBarControlBeta.Properties.EndColor = System.Drawing.Color.Lime;
            this.progressBarControlBeta.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControlBeta.Properties.StartColor = System.Drawing.Color.Green;
            this.progressBarControlBeta.Size = new System.Drawing.Size(326, 27);
            this.progressBarControlBeta.TabIndex = 11;
            // 
            // labelBeta
            // 
            this.labelBeta.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelBeta.Location = new System.Drawing.Point(16, 10);
            this.labelBeta.Name = "labelBeta";
            this.labelBeta.Size = new System.Drawing.Size(201, 25);
            this.labelBeta.TabIndex = 9;
            this.labelBeta.Text = "BETA posodobitve";
            this.labelBeta.UseCompatibleTextRendering = true;
            // 
            // downloadBetaBt
            // 
            this.downloadBetaBt.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("downloadBetaBt.ImageOptions.Image")));
            this.downloadBetaBt.Location = new System.Drawing.Point(406, 48);
            this.downloadBetaBt.Name = "downloadBetaBt";
            this.downloadBetaBt.Size = new System.Drawing.Size(186, 27);
            this.downloadBetaBt.TabIndex = 8;
            this.downloadBetaBt.Text = "Prenesi najnovejše posodobitve";
            this.downloadBetaBt.Click += new System.EventHandler(this.ButtonUpdateBeta_Click);
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.simpleButtonBetaCleanVersion);
            this.panel1.Controls.Add(this.selectBetaVersionBt);
            this.panel1.Controls.Add(this.labelBetaBMC);
            this.panel1.Controls.Add(this.progressBarControlBeta);
            this.panel1.Controls.Add(this.downloadBetaBt);
            this.panel1.Controls.Add(this.labelBeta);
            this.panel1.Location = new System.Drawing.Point(-6, 242);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(609, 95);
            this.panel1.TabIndex = 15;
            // 
            // simpleButtonBetaCleanVersion
            // 
            this.simpleButtonBetaCleanVersion.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButtonBetaCleanVersion.ImageOptions.Image")));
            this.simpleButtonBetaCleanVersion.Location = new System.Drawing.Point(295, 14);
            this.simpleButtonBetaCleanVersion.Name = "simpleButtonBetaCleanVersion";
            this.simpleButtonBetaCleanVersion.Size = new System.Drawing.Size(105, 23);
            this.simpleButtonBetaCleanVersion.TabIndex = 14;
            this.simpleButtonBetaCleanVersion.Text = "Počisti verzijo";
            this.simpleButtonBetaCleanVersion.Click += new System.EventHandler(this.ButtonClearBetaVersion_Click);
            // 
            // selectBetaVersionBt
            // 
            this.selectBetaVersionBt.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("selectBetaVersionBt.ImageOptions.Image")));
            this.selectBetaVersionBt.Location = new System.Drawing.Point(406, 14);
            this.selectBetaVersionBt.Name = "selectBetaVersionBt";
            this.selectBetaVersionBt.Size = new System.Drawing.Size(186, 23);
            this.selectBetaVersionBt.TabIndex = 13;
            this.selectBetaVersionBt.Text = "Izberi verzijo za prenos";
            this.selectBetaVersionBt.Click += new System.EventHandler(this.ButtonSelectBetaVersion_Click);
            // 
            // labelBetaBMC
            // 
            this.labelBetaBMC.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelBetaBMC.Location = new System.Drawing.Point(17, 50);
            this.labelBetaBMC.Name = "labelBetaBMC";
            this.labelBetaBMC.Size = new System.Drawing.Size(51, 21);
            this.labelBetaBMC.TabIndex = 12;
            this.labelBetaBMC.Text = "BMC";
            // 
            // labelCurrentVersion
            // 
            this.labelCurrentVersion.AutoSize = true;
            this.labelCurrentVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F);
            this.labelCurrentVersion.Location = new System.Drawing.Point(12, 76);
            this.labelCurrentVersion.Name = "labelCurrentVersion";
            this.labelCurrentVersion.Size = new System.Drawing.Size(150, 18);
            this.labelCurrentVersion.TabIndex = 16;
            this.labelCurrentVersion.Text = "Trenutna verzija BMC";
            // 
            // selectStableVersionBt
            // 
            this.selectStableVersionBt.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButtonSelectStable.ImageOptions.Image")));
            this.selectStableVersionBt.Location = new System.Drawing.Point(400, 4);
            this.selectStableVersionBt.Name = "selectStableVersionBt";
            this.selectStableVersionBt.Size = new System.Drawing.Size(186, 23);
            this.selectStableVersionBt.TabIndex = 17;
            this.selectStableVersionBt.Text = "Izberi verzijo za prenos";
            this.selectStableVersionBt.Click += new System.EventHandler(this.ButtonSelectStableVersion_Click);
            // 
            // simpleButtonStableCleanVersion
            // 
            this.simpleButtonStableCleanVersion.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButtonStableCleanVersion.ImageOptions.Image")));
            this.simpleButtonStableCleanVersion.Location = new System.Drawing.Point(289, 4);
            this.simpleButtonStableCleanVersion.Name = "simpleButtonStableCleanVersion";
            this.simpleButtonStableCleanVersion.Size = new System.Drawing.Size(105, 23);
            this.simpleButtonStableCleanVersion.TabIndex = 18;
            this.simpleButtonStableCleanVersion.Text = "Počisti verzijo";
            this.simpleButtonStableCleanVersion.Click += new System.EventHandler(this.ButtonClearStableVersion_Click);
            // 
            // labelStable
            // 
            this.labelStable.Font = new System.Drawing.Font("Microsoft Sans Serif", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labelStable.Location = new System.Drawing.Point(10, 4);
            this.labelStable.Name = "labelStable";
            this.labelStable.Size = new System.Drawing.Size(231, 25);
            this.labelStable.TabIndex = 19;
            this.labelStable.Text = "STABLE posodobitve";
            this.labelStable.UseCompatibleTextRendering = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(598, 338);
            this.Controls.Add(this.labelStable);
            this.Controls.Add(this.simpleButtonStableCleanVersion);
            this.Controls.Add(this.selectStableVersionBt);
            this.Controls.Add(this.labelCurrentVersion);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.progressBarControl2);
            this.Controls.Add(this.downloadWebparamBT);
            this.Controls.Add(this.progressBarControl1);
            this.Controls.Add(this.labelStableBMC);
            this.Controls.Add(this.downloadStableBt);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Updater";
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControlBeta.Properties)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraEditors.SimpleButton downloadStableBt;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Label labelStableBMC;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControl1;
        private DevExpress.XtraEditors.SimpleButton downloadWebparamBT;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControl2;
        private System.Windows.Forms.Label label3;
        private System.ComponentModel.BackgroundWorker backgroundWorker2;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControlBeta;
        private System.Windows.Forms.Label labelBeta;
        private DevExpress.XtraEditors.SimpleButton downloadBetaBt;
        private System.Windows.Forms.Panel panel1;
        private System.ComponentModel.BackgroundWorker backgroundWorker3;
        private System.ComponentModel.BackgroundWorker backgroundWorker4;
        private System.Windows.Forms.Label labelBetaBMC;
        private DevExpress.XtraEditors.SimpleButton selectBetaVersionBt;
        private System.Windows.Forms.Label labelCurrentVersion;
        private DevExpress.XtraEditors.SimpleButton simpleButtonBetaCleanVersion;
        private DevExpress.XtraEditors.SimpleButton selectStableVersionBt;
        private DevExpress.XtraEditors.SimpleButton simpleButtonStableCleanVersion;
        private System.Windows.Forms.Label labelStable;
    }
}

