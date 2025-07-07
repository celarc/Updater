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
            this.simpleButton1 = new DevExpress.XtraEditors.SimpleButton();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.progressBarControl1 = new DevExpress.XtraEditors.ProgressBarControl();
            this.simpleButton2 = new DevExpress.XtraEditors.SimpleButton();
            this.progressBarControl2 = new DevExpress.XtraEditors.ProgressBarControl();
            this.label3 = new System.Windows.Forms.Label();
            this.backgroundWorker2 = new System.ComponentModel.BackgroundWorker();
            this.progressBarControlBeta = new DevExpress.XtraEditors.ProgressBarControl();
            this.labelBeta = new System.Windows.Forms.Label();
            this.downloadBetaBt = new DevExpress.XtraEditors.SimpleButton();
            this.panel1 = new System.Windows.Forms.Panel();
            this.selectBetaVersionBt = new DevExpress.XtraEditors.SimpleButton();
            this.labelBetaBMC = new System.Windows.Forms.Label();
            this.backgroundWorker3 = new System.ComponentModel.BackgroundWorker();
            this.backgroundWorker4 = new System.ComponentModel.BackgroundWorker();
            this.labelCurrentVerzija = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControl2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.progressBarControlBeta.Properties)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // simpleButton1
            // 
            this.simpleButton1.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton1.ImageOptions.Image")));
            this.simpleButton1.Location = new System.Drawing.Point(457, 37);
            this.simpleButton1.Name = "simpleButton1";
            this.simpleButton1.Size = new System.Drawing.Size(129, 27);
            this.simpleButton1.TabIndex = 0;
            this.simpleButton1.Text = "Preveri posodobitve";
            this.simpleButton1.Click += new System.EventHandler(this.simpleButton1_Click);
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 18);
            this.label1.TabIndex = 1;
            this.label1.Text = "Aplikacija";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label2.Location = new System.Drawing.Point(12, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(42, 18);
            this.label2.TabIndex = 3;
            this.label2.Text = "BMC";
            // 
            // progressBarControl1
            // 
            this.progressBarControl1.Location = new System.Drawing.Point(106, 37);
            this.progressBarControl1.Name = "progressBarControl1";
            this.progressBarControl1.Properties.EndColor = System.Drawing.Color.Lime;
            this.progressBarControl1.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControl1.Properties.StartColor = System.Drawing.Color.Green;
            this.progressBarControl1.Size = new System.Drawing.Size(345, 27);
            this.progressBarControl1.TabIndex = 4;
            // 
            // simpleButton2
            // 
            this.simpleButton2.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton2.ImageOptions.Image")));
            this.simpleButton2.Location = new System.Drawing.Point(457, 70);
            this.simpleButton2.Name = "simpleButton2";
            this.simpleButton2.Size = new System.Drawing.Size(129, 27);
            this.simpleButton2.TabIndex = 5;
            this.simpleButton2.Text = "Preveri posodobitve";
            this.simpleButton2.Click += new System.EventHandler(this.simpleButton2_Click);
            // 
            // progressBarControl2
            // 
            this.progressBarControl2.Location = new System.Drawing.Point(106, 70);
            this.progressBarControl2.Name = "progressBarControl2";
            this.progressBarControl2.Properties.EndColor = System.Drawing.Color.Lime;
            this.progressBarControl2.Properties.LookAndFeel.UseDefaultLookAndFeel = false;
            this.progressBarControl2.Properties.StartColor = System.Drawing.Color.Green;
            this.progressBarControl2.Size = new System.Drawing.Size(345, 27);
            this.progressBarControl2.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.label3.Location = new System.Drawing.Point(12, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(85, 18);
            this.label3.TabIndex = 7;
            this.label3.Text = "Web param";
            // 
            // backgroundWorker2
            // 
            this.backgroundWorker2.WorkerReportsProgress = true;
            this.backgroundWorker2.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker2_DoWork);
            this.backgroundWorker2.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker2_ProgressChanged);
            this.backgroundWorker2.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker2_RunWorkerCompleted);
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
            this.downloadBetaBt.Click += new System.EventHandler(this.simpleButtonBeta_Click);
            // 
            // panel1
            // 
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
            // selectBetaVersionBt
            // 
            this.selectBetaVersionBt.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("selectBetaVersionBt.ImageOptions.Image")));
            this.selectBetaVersionBt.Location = new System.Drawing.Point(406, 14);
            this.selectBetaVersionBt.Name = "selectBetaVersionBt";
            this.selectBetaVersionBt.Size = new System.Drawing.Size(186, 23);
            this.selectBetaVersionBt.TabIndex = 13;
            this.selectBetaVersionBt.Text = "Izberi verzijo za prenos";
            this.selectBetaVersionBt.Click += new System.EventHandler(this.simpleButton3_Click);
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
            // backgroundWorker3
            // 
            this.backgroundWorker3.WorkerReportsProgress = true;
            this.backgroundWorker3.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker3_DoWork);
            this.backgroundWorker3.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker3_ProgressChanged);
            this.backgroundWorker3.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker3_RunWorkerCompleted);
            // 
            // labelCurrentVerzija
            // 
            this.labelCurrentVerzija.AutoSize = true;
            this.labelCurrentVerzija.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCurrentVerzija.Location = new System.Drawing.Point(87, 7);
            this.labelCurrentVerzija.Name = "labelCurrentVerzija";
            this.labelCurrentVerzija.Size = new System.Drawing.Size(121, 20);
            this.labelCurrentVerzija.TabIndex = 16;
            this.labelCurrentVerzija.Text = "Trenutna verzija";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(598, 338);
            this.Controls.Add(this.labelCurrentVerzija);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.progressBarControl2);
            this.Controls.Add(this.simpleButton2);
            this.Controls.Add(this.progressBarControl1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.simpleButton1);
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

        private DevExpress.XtraEditors.SimpleButton simpleButton1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private DevExpress.XtraEditors.ProgressBarControl progressBarControl1;
        private DevExpress.XtraEditors.SimpleButton simpleButton2;
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
        private System.Windows.Forms.Label labelCurrentVerzija;
    }
}

