using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        public static void Main(string[] args)
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

            var indexCleanup = args.Select((v, i) => i).FirstOrDefault(i =>
                args[i].Equals("/cleanup", StringComparison.InvariantCultureIgnoreCase) ||
                args[i].Equals("-cleanup", StringComparison.InvariantCultureIgnoreCase) ||
                args[i].Equals("--cleanup", StringComparison.InvariantCultureIgnoreCase)
            , -1);

            if (indexCleanup >= 0 && indexCleanup < args.Length - 1)
            {
                var cleanupPathIndex = indexCleanup + 1;
                var cleanupPath = args[cleanupPathIndex];

                Exception? cleanedUpException = null;

                for (var @try = 0; @try < 30; ++@try)
                {
                    try
                    {
                        File.Delete(cleanupPath);
                        cleanedUpException = null;
                        break;
                    }
                    catch (Exception ex)
                    {
                        cleanedUpException = ex;
                    }

                    // Wait a short while until the setup file lock is released
                    Thread.Sleep(100);
                }

                if (cleanedUpException != null)
                    MessageBox.Show(cleanedUpException.ToString(), "Failed to clean up", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Process.Start(Application.ExecutablePath);
                return;
            }

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
