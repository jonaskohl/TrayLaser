using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaserPointer
{
    public static class Program
    {
        const string MTXID = "de.jonaskohl.TrayLaser.Mutex.FE7A0A2A25C27444B08A3DF502527560";

        [STAThread]
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            using (Mutex mutex = new Mutex(true, MTXID, out bool createdNew))
            {
                if (createdNew)
                {
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);
                    Application.Run(new TrayIconForm());
                } else
                {
                    MessageBox.Show("An instance is already running!", MTXID);
                }
            }
        }
    }
}
