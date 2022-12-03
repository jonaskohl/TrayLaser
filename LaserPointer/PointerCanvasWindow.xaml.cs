using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LaserPointer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class PointerCanvasWindow : Window
    {
        public PointerCanvasWindow()
        {
            SourceInitialized += Window_SourceInitialized;
            InitializeComponent();
            Title = GetType().FullName ?? "";

            Closed += PointerCanvasWindow_Closed;
        }

        private void PointerCanvasWindow_Closed(object? sender, EventArgs e)
        {
            MouseHook.Stop();
            MouseHook.MouseAction -= MouseHook_MouseAction;
        }

        Native.RECT lastMonitorRect = Native.RECT.Empty;
        private void PositionWindowOnRightScreen()
        {
            if (Native.GetCursorPos(out Native.POINT pt))
            {
                var hMonitor = Native.MonitorFromPoint(pt, Native.MONITOR_DEFAULTTONEAREST);
                var info = new Native.MONITORINFOEX();
                if (Native.GetMonitorInfo(hMonitor, info))
                {
                    if (info.rcWork != lastMonitorRect)
                    {
                        Debug.WriteLine("Moved to new screen!");
                        Top = info.rcWork.top;
                        Left = info.rcWork.left;
                    }
                    lastMonitorRect = info.rcWork;
                }
                else
                    Debug.WriteLine("GetMonitorInfo failed!");
            }
            else
                Debug.WriteLine("GetCursorPos failed!");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            PositionWindowOnRightScreen();

            int exStyle = (int)Native.GetWindowLong(wndHelper.Handle, (int)Native.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)Native.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            Native.SetWindowLong(wndHelper.Handle, (int)Native.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            WindowState = WindowState.Maximized;
        }

        void Window_SourceInitialized(object? sender, EventArgs e)
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(handle)?.AddHook(WindowProc);

            MouseHook.MouseAction += MouseHook_MouseAction;
            MouseHook.Start();
        }

        private void MouseHook_MouseAction(object? sender, EventArgs e)
        {
            PositionWindowOnRightScreen();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    Native.WmGetMinMaxInfo(hwnd, lParam, (int)MinWidth, (int)MinHeight);
                    handled = true;
                    break;
            }

            return IntPtr.Zero;
        }

        private void MoveLaserTo(double x, double y)
        {
            var w = laser.ActualWidth;
            var h = laser.ActualHeight;
            laser.SetValue(Canvas.LeftProperty, x - w / 2d);
            laser.SetValue(Canvas.TopProperty, y - h / 2d);
        }

        private void LaserMouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null)
            {
                return;
            }

            var mp = Mouse.GetPosition(canvas);
            MoveLaserTo(mp.X, mp.Y);
            SetLaserScale(e.RightButton == MouseButtonState.Pressed ? 0.125d : (e.LeftButton == MouseButtonState.Pressed ? 2 : 1));
        }

        private void LaserStylusMove(object sender, StylusEventArgs e)
        {
            if (!e.InAir)
            {
                var pts = e.GetStylusPoints(canvas);
                if (pts.Count > 0)
                {
                    var pt = pts.First();
                    MoveLaserTo(pt.X, pt.Y);
                    if (e.StylusDevice.TabletDevice.TabletHardwareCapabilities.HasFlag(TabletHardwareCapabilities.SupportsPressure) && pt.Description.HasProperty(StylusPointProperties.NormalPressure))
                    {
                        var pressInfo = pt.Description.GetPropertyInfo(StylusPointProperties.NormalPressure);
                        var min = pressInfo.Minimum;
                        var max = pressInfo.Maximum;
                        var val = pt.GetPropertyValue(StylusPointProperties.NormalPressure);
                        var curr = (val - min) / ((double)max / 2);
                        SetLaserScale(curr);
                    }
                    else
                    {
                        SetLaserScale(1);
                    }
                    return;
                }
            }

            var pos = e.GetPosition(canvas);
            MoveLaserTo(pos.X, pos.Y);
            SetLaserScale(1);
        }

        private void SetLaserScale(double scale)
        {
            laserScale.ScaleX = laserScale.ScaleY = scale;
        }

        private void Window_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void Window_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void LaserMouseButtonChanged(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null) return;
            SetLaserScale(e.RightButton == MouseButtonState.Pressed ? 0.125d : (e.LeftButton == MouseButtonState.Pressed ? 2 : 1));
        }
    }
}
