using System;
using System.Windows.Forms;

namespace Updater
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // Vklopi TLS 1.2 za HTTPS klice na update.php - na starejsih Windows postajah
            // .NET 4.7.2 sicer lahko pade pri HTTPS rokovanju (OR ne pokvari novejsih protokolov).
            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
