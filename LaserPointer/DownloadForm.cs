using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Security.Policy;
using System.Windows.Threading;
using System.IO;
using System.Diagnostics;

namespace LaserPointer
{
    public partial class DownloadForm : Form
    {
        HttpClient client;
        long bytesRecieved = 0;
        bool cancelRequested = false;
        BreakReason breakReason;

        string targetName;

        enum BreakReason
        {
            Unknown,
            UserCancel,
            Finished,
            Error
        }

        public DownloadForm()
        {
            InitializeComponent();
            client = new();

            targetName = Path.Combine(Environment.GetEnvironmentVariable("temp") ?? @"C:\", "JK-TEMP-" + Guid.NewGuid().ToString() + ".exe");
        }

        public void DownloadUpdateModal(Form owner, string binary)
        {
            BeginDownload(binary);
            ShowDialog(owner);
        }

        private async void BeginDownload(string url)
        {
            using (var targetFile = File.OpenWrite(targetName))
            using (var tbw = new BinaryWriter(targetFile))
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    while (true)
                    {
                        if (cancelRequested)
                        {
                            breakReason = BreakReason.UserCancel;
                            break;
                        }

                        var buff = new byte[4096];
                        var read = stream.Read(buff, 0, buff.Length);

                        if (read < 1)
                        {
                            breakReason = BreakReason.Finished;
                            break;
                        }

                        tbw.Write(buff, 0, read);

                        long? totalBytes = response.Content.Headers.ContentLength;
                        bytesRecieved += read;

                        ReportProgress(bytesRecieved, totalBytes);
                    }
                }
            }

            if (breakReason == BreakReason.UserCancel)
            {
                Close();
            } else if (breakReason == BreakReason.Finished)
            {
                Process.Start(targetName);//, "/SP- /SILENT /NOCANCEL");
                Application.Exit();
            }
        }

        private void ReportProgress(long bytesRecieved, long? totalBytes)
        {
            Invoke(() =>
            {
                if (totalBytes.HasValue)
                {
                    progressBar1.Maximum = (int)totalBytes;
                    progressBar1.Minimum = 0;
                    progressBar1.Value = (int)bytesRecieved;
                    progressBar1.Style = ProgressBarStyle.Continuous;
                }
                else
                {
                    progressBar1.Style = ProgressBarStyle.Marquee;
                }
                Application.DoEvents();
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            cancelRequested = true;
        }
    }
}
