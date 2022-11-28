using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;

namespace LaserPointer
{
    public partial class TrayIconForm : Form
    {
        PointerCanvasWindow? pointerCanvasWindow = null;

        const int WM_SETTINGCHANGE = 0x1A;
        const string THEME_SETTING = "ImmersiveColorSet";

        public TrayIconForm()
        {
            InitializeComponent();
            Text = GetType().FullName ?? "";
            HandleCreated += TrayIconForm_HandleCreated;
            Load += TrayIconForm_Load;
            Shown += TrayIconForm_Shown;
            Opacity = 0;

            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        ~TrayIconForm()
        {
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Resume)
            {
                _ = UpdateCheck.CheckForUpdateUI(this, false);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SETTINGCHANGE)
            {
                var setting = Marshal.PtrToStringAuto(m.LParam);
                if (setting == THEME_SETTING)
                {
                    OnDarkModeChanged();
                }
            }
            base.WndProc(ref m);
        }

        private void OnDarkModeChanged()
        {
            UpdateTrayIcon(GetSystemIsDark());
        }

        private void UpdateTrayIcon(bool dark)
        {
            var active = pointerCanvasWindow != null;
            notifyIcon1.Icon = dark ? (
                active ? Properties.Resources.TrayIcon_DarkMode_Active : Properties.Resources.TrayIcon_DarkMode_Inactive
            ) : (
                active ? Properties.Resources.TrayIcon_LightMode_Active : Properties.Resources.TrayIcon_LightMode_Inactive
            );
        }

        private bool GetSystemIsDark()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                var val = (int?)key?.GetValue("SystemUsesLightTheme", 1) ?? 1;
                return val == 0;
            }
        }

        private void TrayIconForm_Shown(object? sender, EventArgs e)
        {
            Hide();
            _ = UpdateCheck.CheckForUpdateUI(this, false);
        }

        private void TrayIconForm_Load(object? sender, EventArgs e)
        {
            UpdateTrayIcon(GetSystemIsDark());
        }

        private void TrayIconForm_HandleCreated(object? sender, EventArgs e)
        {
            int exStyle = (int)Native.GetWindowLong(Handle, (int)Native.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)Native.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            Native.SetWindowLong(Handle, (int)Native.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            pointerCanvasWindow?.Close();
            base.OnFormClosing(e);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (pointerCanvasWindow == null)
                {
                    pointerCanvasWindow = new PointerCanvasWindow();
                    pointerCanvasWindow.Show();
                }
                else
                {
                    pointerCanvasWindow.Close();
                    pointerCanvasWindow = null;
                }
            }
            UpdateTrayIcon(GetSystemIsDark());
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string VISIT_TEXT = "Visit project page";
            var visitButton = new TaskDialogButton(VISIT_TEXT);

            var asm = Assembly.GetExecutingAssembly();
            var version = asm?.GetName()?.Version?.ToString() ?? "<UNKNOWN_VERSION>";
            var copyright = asm != null ? FileVersionInfo.GetVersionInfo(asm!.Location).LegalCopyright : "<UNKNOWN_COPYRIGHT>";

            var t = new TaskDialogPage()
            {
                Caption = "About TrayLaser",
                Heading = "TrayLaser",
                Text = $"Version {version}\r\n{copyright}",
                Buttons = new TaskDialogButtonCollection()
                {
                    visitButton,
                    TaskDialogButton.OK
                },
                DefaultButton = TaskDialogButton.OK,
                Icon = TaskDialogIcon.Information,
            };
            var hWnd = IntPtr.Zero;
            if (pointerCanvasWindow != null)
            {
                hWnd = new WindowInteropHelper(pointerCanvasWindow).Handle;
            }
            var b = TaskDialog.ShowDialog(hWnd, t, TaskDialogStartupLocation.CenterScreen);
            if (b == visitButton)
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer",
                    Arguments = "\"https://jonaskohl.de/goto.php?t=tl\"",
                    UseShellExecute = false
                });
        }

        private void checkForUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _ = UpdateCheck.CheckForUpdateUI(this);
        }
    }
}
