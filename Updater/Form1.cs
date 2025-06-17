using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using WinSCP;
using static System.Net.WebRequestMethods;

namespace Updater
{

    public partial class Form1 : Form
    {
        private string potBMC = @"C:\BMC\", potWebParam = @"C:\inetpub\WebParam\";
        private int percent = 0;
        private bool autoUpdate = false;
        private string autoUpdateLog = "", autoUpdateWebLog = "";

        public Form1()
        {
            InitializeComponent();
            panel1.Visible = false;
            initPath();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ukazCMD = Environment.GetCommandLineArgs();
            var cmd = ukazCMD.ToList();
            cmd.Add("updateBeta");
            ukazCMD = cmd.ToArray(); // za testiranje, da se lahko sproži BETA posodobitev
            if (ukazCMD.Contains("updateBMC"))
            {
                try
                {
                    autoUpdateLog += "Začetek avtomatske posodobitve ob " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine;
                    Process[] processes = Process.GetProcessesByName("BMC");
                    int i = 0;
                    foreach (var process in processes)
                    {
                        i++;
                        process.Kill();
                    }
                    if (i > 0) autoUpdateLog += "Pred prenosom podatkov sem zaustavil odprete programe, število odprtih programov je bilo: " + i + Environment.NewLine;
                    autoUpdate = true;
                    autoUpdateLog += "Začetek prenosa podatkov" + Environment.NewLine;
                    backgroundWorker1.RunWorkerAsync();
                }
                catch { this.Close(); }
            }
            if (ukazCMD.Contains("updateWebParam"))
            {
                try
                {
                    autoUpdateLog += "Začetek avtomatske posodobitve ob " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine;
                    Process[] processes = Process.GetProcessesByName("WebParam");
                    int i = 0;
                    foreach (var process in processes)
                    {
                        i++;
                        process.Kill();
                    }
                    if (i > 0) autoUpdateLog += "Pred prenosom podatkov sem zaustavil odprete programe, število odprtih programov je bilo: " + i + Environment.NewLine;
                    autoUpdate = true;
                    autoUpdateLog += "Začetek prenosa podatkov" + Environment.NewLine;
                    backgroundWorker2.RunWorkerAsync();
                }
                catch { this.Close(); }
            }
            if (ukazCMD.Contains("updateBETA")) { panel1.Visible = true; }
        }
        private void initPath()
        {
            try
            {
                XmlDataDocument xmldoc = new XmlDataDocument();
                XmlNodeList xmlnode;
                FileStream fs = new FileStream(@"BMC.ini", FileMode.Open, FileAccess.Read);
                xmldoc.Load(fs);
                fs.Close();

                try
                {
                    xmlnode = xmldoc.GetElementsByTagName("POT_BMC");
                    potBMC = xmlnode[0].InnerText;
                    if (potBMC.Length == 0) potBMC = "C:\\BMC\\";
                }
                catch { }
                try
                {
                    xmlnode = xmldoc.GetElementsByTagName("POT_WEB_PARAM");
                    potWebParam = xmlnode[0].InnerText;
                    if (potWebParam.Length == 0) potWebParam = "C:\\inetpub\\WebParam\\";
                }
                catch { }
            }
            catch { }
        }

        private void simpleButtonBeta_Click(object sender, EventArgs e)
        {
            var warningText =
                "To je BETA različica posodobitve. Nekatere funkcije morda ne bodo delovale pravilno.\n\n" +
                "Želite vseeno nadaljevati?";
            var warningResult = MessageBox.Show(
                warningText,
                "Opozorilo – BETA",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);
            if (warningResult != DialogResult.OK) return;

            if (Process.GetProcessesByName("BMC").Any())
            {
                MessageBox.Show("Zaprite vse odprte BMC programe!");
                return;
            }

            simpleButtonBeta.Enabled = false;
            progressBarControlBeta.EditValue = 0;
            backgroundWorker3.RunWorkerAsync();
        }


        private void getFromFTP()
        {
            percent = 0;
            lendth = 0;
            i = 0;
            simpleButton1.Enabled = false;
            progressBarControl1.Properties.Step = 1;
            progressBarControl1.Properties.PercentView = true;
            progressBarControl1.Properties.Maximum = 100;
            progressBarControl1.Properties.Minimum = 0;
            progressBarControl1.EditValue = 0;
            progressBarControl1.Properties.PercentView = true;
            progressBarControl1.Properties.ShowTitle = true;

            backgroundWorker1.RunWorkerAsync();
        }
        private void getFromFTPWebParam()
        {
            percent = 0;
            lendth = 0;
            i = 0;
            simpleButton2.Enabled = false;
            progressBarControl2.Properties.Step = 1;
            progressBarControl2.Properties.PercentView = true;
            progressBarControl2.Properties.Maximum = 100;
            progressBarControl2.Properties.Minimum = 0;
            progressBarControl2.EditValue = 0;
            progressBarControl2.Properties.PercentView = true;
            progressBarControl2.Properties.ShowTitle = true;

            backgroundWorker2.RunWorkerAsync();
        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessesByName("BMC").Count() > 0) MessageBox.Show("Zaprite vse oprte BMC programe!");
            else
            {
                string message = "Ali preverim in prenesem posodobitve za program BMC?";
                string caption = "Potrdi";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                DialogResult result;

                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    getFromFTP();
                }

            }
        }
        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessesByName("WebParam").Count() > 0) MessageBox.Show("Zaprite vse oprte Web Param programe!");
            else
            {
                string message = "Ali preverim in prenesem posodobitve za program WebParam?";
                string caption = "Potrdi";
                MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
                DialogResult result;

                result = MessageBox.Show(message, caption, buttons);

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    getFromFTPWebParam();
                }

            }
        }
        private double lendth = 0, i = 1;
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            try
            {

                string url = "bmc.si", username = "updater@bmc.si", password = "fcc1b727289ac03db7e76f6291039923";

                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = url,
                    UserName = username,
                    Password = password,
                };

                using (Session session = new Session())
                {
                    string localPath = potBMC;
                    // Connect
                    session.Open(sessionOptions);

                    string remotePath = @"/1/";
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(remotePath);
                    lendth = directoryInfo.Files.Count;

                    downloadFiles(directoryInfo, session, localPath, remotePath, backgroundWorker, true);
                }
            }
            catch (Exception ex)
            {
                backgroundWorker.ReportProgress(-1, ex);
            }
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            try
            {

                string url = "bmc.si", username = "updater@bmc.si", password = "fcc1b727289ac03db7e76f6291039923";

                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = url,
                    UserName = username,
                    Password = password,
                };

                using (Session session = new Session())
                {
                    string localPath = potBMC;
                    session.Open(sessionOptions);

                    string remotePath = @"/BETA/";
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(remotePath);
                    lendth = directoryInfo.Files.Count;

                    downloadFiles(directoryInfo, session, localPath, remotePath, backgroundWorker, true);
                }
            }
            catch (Exception ex)
            {
                backgroundWorker.ReportProgress(-1, ex);
            }
        }

        private void backgroundWorker3_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage < 0)
            {
                var ex = (Exception)e.UserState;
                MessageBox.Show(ex.ToString());
            }
            else
            {
                progressBarControlBeta.EditValue = e.ProgressPercentage;
                percent = e.ProgressPercentage;
            }
        }


        private void backgroundWorker3_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            simpleButtonBeta.Enabled = true;

            if (e.Error != null)
            {
                MessageBox.Show("Prišlo je do napake: " + e.Error.Message);
            }
            else if (e.Cancelled)
            {
                MessageBox.Show("Prenos je bil preklican.");
            }
            else
            {
                MessageBox.Show("Prenos BETA aplikacije končan!");
            }
        }


        private void downloadFiles(RemoteDirectoryInfo directoryInfo, Session session, string localPath, string remotePath, BackgroundWorker backgroundWorker, bool rootLevel)
        {

            foreach (RemoteFileInfo rfi in directoryInfo.Files)
            {
                if (rfi.IsDirectory && rfi.Name.Replace(".", "").Length > 0)
                {
                    downloadFiles(session.ListDirectory(remotePath + rfi.Name + "/"), session, localPath + rfi.Name + "\\", remotePath + rfi.Name + "/", backgroundWorker, false);
                }
                else
                {//MessageBox.Show(rfi.Name + " " + rfi.LastWriteTime + " Time local: " + System.IO.File.GetLastWriteTime(localPath + rfi.Name) + "     Type:" + rfi.FileType);
                    if (rfi.LastWriteTime != System.IO.File.GetLastWriteTime(localPath + rfi.Name) && rfi.Name.Replace(".", "").Length > 0 && rfi.Name.Split('.').Last() != "fdb" && rfi.Name.Split('.').Last() != "FDB")
                    {
                        string sourcePath = RemotePath.EscapeFileMask(remotePath + rfi.Name);
                        TransferOperationResult transferResult = session.GetFiles(sourcePath, localPath);
                        transferResult.Check();

                        foreach (TransferEventArgs transfer in transferResult.Transfers)
                        {
                            autoUpdateLog += "Prenos datoteke " + transfer.FileName + " uspešen." + Environment.NewLine;
                        }
                    }
                }

                if (rootLevel)
                {
                    i++;
                    backgroundWorker.ReportProgress(Convert.ToInt32((i / lendth) * 100));
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            if (e.ProgressPercentage == -1)
            {
                Exception ex = (Exception)e.UserState;
                MessageBox.Show(ex.Message + Environment.NewLine + Environment.NewLine + ex.InnerException + Environment.NewLine + Environment.NewLine + ex.Source + Environment.NewLine + Environment.NewLine + ex.StackTrace + Environment.NewLine + Environment.NewLine + ex.TargetSite);
            }
            else
            {
                progressBarControl1.EditValue = e.ProgressPercentage;
                //progressBarControl1.PerformStep();
                percent = e.ProgressPercentage;
                progressBarControl1.Update();
            }

        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker b1 = (BackgroundWorker)sender;
            if (autoUpdate)
            {
                if (percent != 100) autoUpdateLog += "Med prenosom je prišlo do napake.";
                else autoUpdateLog += "Posodobitve uspešno prenešene, konec prenosa podatkov ob: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine + Environment.NewLine;

                try
                {
                    if (!Directory.Exists(potBMC + @"Log\")) Directory.CreateDirectory(potBMC + @"Log\");
                    System.IO.File.AppendAllText(potBMC + @"Log\AutoUpdate.txt", autoUpdateLog);
                }
                catch { }
                this.Close();
            }
            else
            {
                if (percent != 100) MessageBox.Show("Med prenosom je prišlo do napake. Preverite internetno povezavo in preverite če je kje še kje odprt BMC program.");
                else MessageBox.Show("Prenos končan!");
                simpleButton1.Enabled = true;
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            var backgroundWorker = sender as BackgroundWorker;
            try
            {
                string url = "bmc.si", username = "updater@bmc.si", password = "fcc1b727289ac03db7e76f6291039923";

                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = url,
                    UserName = username,
                    Password = password,
                };

                using (Session session = new Session())
                {
                    string localPath = potWebParam;
                    // Connect
                    session.Open(sessionOptions);

                    string remotePath = @"/6/";
                    RemoteDirectoryInfo directoryInfo = session.ListDirectory(remotePath);

                    lendth = directoryInfo.Files.Count;
                    int i = 0;


                    downloadFiles(directoryInfo, session, localPath, remotePath, backgroundWorker, true);
                }
            }
            catch (Exception ex) { backgroundWorker.ReportProgress(-1, ex); }
        }

        private void backgroundWorker2_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == -1)
            {
                Exception ex = (Exception)e.UserState;
                MessageBox.Show(ex.Message);
            }
            else
            {
                progressBarControl2.EditValue = e.ProgressPercentage;
                //progressBarControl1.PerformStep();
                percent = e.ProgressPercentage;
                progressBarControl2.Update();
            }
        }

        private void backgroundWorker2_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (autoUpdate)
            {
                if (percent != 100) autoUpdateWebLog += "Med prenosom je prišlo do napake.";
                else autoUpdateWebLog += "Posodobitve uspešno prenešene, konec prenosa podatkov ob: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + Environment.NewLine + Environment.NewLine;

                try
                {
                    if (!Directory.Exists(potWebParam + @"Log\")) Directory.CreateDirectory(potWebParam + @"Log\");
                    System.IO.File.AppendAllText(potWebParam + @"Log\AutoUpdate.txt", autoUpdateWebLog);
                }
                catch { }
                this.Close();
            }
            else
            {
                if (percent != 100) MessageBox.Show("Med prenosom je prišlo do napake. Preverite internetno povezavo in preverite če je kje še kje odprt BMC program.");
                else MessageBox.Show("Prenos končan!");
                simpleButton1.Enabled = true;
            }
        }
    }
}

