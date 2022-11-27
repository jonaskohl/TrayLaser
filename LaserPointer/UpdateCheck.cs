using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LaserPointer
{
    public static class UpdateCheck
    {
        const string CHECK_URL = @"https://jonaskohl.de/software/traylaser/updatecheck.php";
        const string PRODUCT = "de.jonaskohl.TrayLaser";
        const string ENDPOINT = "27C132BCF123F84EB0728CE83D274D95";
        const string VFORMAT = "1";
        const string XMLNS = "http://www.jonaskohl.de/2022/updatecheck";

        private static Version? GetCurrentAppVersion()
        {
            return Assembly.GetExecutingAssembly()?.GetName()?.Version;
        }

        private static bool IsUpdateCheckDisabled()
        {
            using (var reg = Registry.CurrentUser.OpenSubKey(@"Software\Jonas Kohl\TrayLaser"))
            {
                if (reg != null)
                {
                    return reg.GetValue("DisableUpdateCheck", 0).ToString() == "1";
                }
            }
            return false;
        }

        public static async Task CheckForUpdateUI(Form owner, bool showNoUpdateAvailableMessage = true)
        {
            if (IsUpdateCheckDisabled())
            {
                if (showNoUpdateAvailableMessage)
                    owner.Invoke(() =>
                    {
                        TaskDialog.ShowDialog(owner, new TaskDialogPage()
                        {
                            Caption = "Update check disabled",
                            AllowCancel = true,
                            Buttons = new TaskDialogButtonCollection()
                            {
                                TaskDialogButton.OK
                            },
                            Icon = TaskDialogIcon.Warning,
                            Heading = "Update check disabled",
                            Text = "Checking for updates has been disabled by your administrator."
                        }, TaskDialogStartupLocation.CenterScreen);
                    });
                return;
            }

            UpdateInfo? ui;
            try
            {
                ui = await CheckForUpdate();
            } catch (Exception ex)
            {
                if (showNoUpdateAvailableMessage)
                    owner.Invoke(() =>
                    {
                        TaskDialog.ShowDialog(owner, new TaskDialogPage()
                        {
                            Caption = "An error occurred",
                            AllowCancel = true,
                            Buttons = new TaskDialogButtonCollection()
                            {
                                TaskDialogButton.OK
                            },
                            Icon = TaskDialogIcon.Error,
                            Heading = "An error occurred",
                            Text = "An error occurred while checking for updates!",
                            Expander = new TaskDialogExpander(ex.GetType().FullName + " - " + ex.Message)
                        }, TaskDialogStartupLocation.CenterScreen);
                    });
                return;
            }
            if (ui == null)
            {
                if (showNoUpdateAvailableMessage)
                    owner.Invoke(() =>
                    {
                        TaskDialog.ShowDialog(owner, new TaskDialogPage()
                        {
                            Caption = "No update available",
                            AllowCancel = true,
                            Buttons = new TaskDialogButtonCollection()
                            {
                                TaskDialogButton.OK
                            },
                            Icon = TaskDialogIcon.Information,
                            Heading = "No update available",
                            Text = "You are using the latest version of this software!"
                        }, TaskDialogStartupLocation.CenterScreen);
                    });
            }
            else
            {
                owner.Invoke(() =>
                {
                    var btnUpdateNow = new TaskDialogCommandLinkButton("Update now");
                    var btnViewOnline = new TaskDialogCommandLinkButton("View online");

                    var result = TaskDialog.ShowDialog(owner, new TaskDialogPage()
                    {
                        Caption = "An update is available!",
                        AllowCancel = true,
                        Buttons = new TaskDialogButtonCollection()
                        {
                            btnUpdateNow,
                            btnViewOnline
                        },
                        Icon = TaskDialogIcon.Information,
                        Heading = "An update is available!",
                        Text = $"An update for this software, version {ui.CurrentVersion}, is available!"
                    }, TaskDialogStartupLocation.CenterScreen);

                    if (result == btnUpdateNow)
                    {
                        using var df = new DownloadForm();
                        df.DownloadUpdateModal(owner, ui.Binary);
                    }
                    else if (result == btnViewOnline)
                    {
                        if (ui.DetailsURL.StartsWith("https://"))
                            Process.Start(new ProcessStartInfo()
                            {
                                FileName = ui.DetailsURL,
                                UseShellExecute = true
                            });
                    }
                });
            }
        }

        public static async Task<UpdateInfo?> CheckForUpdate()
        {
            if (IsUpdateCheckDisabled()) return null;

            XNamespace ns = XMLNS;

            var asm = Assembly.GetExecutingAssembly();
            var appname = asm?.GetName().Name ?? "<UNKNOWN>";
            var appver = GetCurrentAppVersion()?.ToString() ?? "<UNKNOWN>";

            var ua = $"{appname}/{appver} Windows/{Environment.OSVersion.Version}";

            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(ua);
            var resp = await client.GetAsync(CHECK_URL);
            resp.EnsureSuccessStatusCode();
            var xmlStr = await resp.Content.ReadAsStringAsync();
            var xdoc = XDocument.Parse(xmlStr);
            Debug.Assert(xdoc.Root?.Name.LocalName == "versioninfo");
            Debug.Assert(xdoc.Root?.Attribute("format")?.Value == VFORMAT);
            Debug.Assert(xdoc.Root?.Attribute("xmlns")?.Value == XMLNS);

            var product = xdoc.Root.Element(ns + "product");
            Debug.Assert(product != null);

            var pname = product.Element(ns + "name")?.Value;
            var pver = product.Element(ns + "currentVersion")?.Value;
            var pbin = product.Element(ns + "binary")?.Value;
            var pdetails = product.Element(ns + "details")?.Value;

            Debug.Assert(pname != null);
            Debug.Assert(pver != null);
            Debug.Assert(pbin != null);
            Debug.Assert(pdetails != null);

            var rver = new Version(pver);

            var cver = GetCurrentAppVersion();
            Debug.Assert(cver != null);

            if (cver >= rver)
                return null;

            return new UpdateInfo()
            {
                Name = pname,
                CurrentVersion = rver,
                Binary = pbin,
                DetailsURL = pdetails
            };
        }
    }

    public class UpdateInfo
    {
        public string Name { get; init; }
        public Version CurrentVersion { get; init; }
        public string Binary { get; init; }
        public string DetailsURL { get; init; }
        // TODO Changelog
    }
}
