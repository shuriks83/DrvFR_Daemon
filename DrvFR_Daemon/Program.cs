using System;
using System.Threading;
using System.Windows.Forms;
using DrvFR_Daemon;

namespace DrvFR_Daemon
{
    static class Program
    {
        private static Mutex singleInstanceMutex;

        [STAThread]
        static void Main()
        {
            bool createdNew;
            singleInstanceMutex = new Mutex(true, "DrvFR_Daemon_SingleInstance", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Приложение уже запущено.", "DrvFR Daemon", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new TrayContext());
            }
            finally
            {
                singleInstanceMutex.ReleaseMutex();
                singleInstanceMutex.Dispose();
            }
        }
    }
}
