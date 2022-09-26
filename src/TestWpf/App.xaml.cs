using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using UIAutomationClient;
using Application = System.Windows.Application;

namespace TestWpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            //LogWindow log = new();
            //log.Show();

            //WpfUiWindow w = new WpfUiWindow();
            //w.Show();

            var hotkeys = new ComponentDispatcherHotkeyCollection();
            hotkeys.Add(Key.V, ModifierKeys.Control | ModifierKeys.Shift, (_, _) =>
            {
                //const string snippet = "MrSnippetier!";
                //ApplySnippet(snippet);

                Test();
            });
        }

        private async static void ApplySnippet(string snippet)
        {
            await Task.Delay(2 * 1000);
            SendKeys.SendWait(snippet);
        }


        private void Test()
        {
            var info = new WinApiProvider.GUITHREADINFO();
            info.cbSize = Marshal.SizeOf(info);
            if (WinApiProvider.GetGUIThreadInfo(0, ref info))
            {
                var hwndFocus = info.hwndFocus;
                var caretRect = GetAccessibleCaretRect(hwndFocus);
                var popup = new LogWindow();

                if (!RectValid(caretRect))
                {
                    // Can't accquire caret placement
                    caretRect = GetWinApiCaretRect(hwndFocus);
                    if (!RectValid(caretRect))
                    {
                        caretRect = new WinApiProvider.RECT()
                        {
                            left = (int)(SystemParameters.PrimaryScreenWidth - popup.Width),
                            top = (int)(SystemParameters.PrimaryScreenHeight - popup.Height),
                            right = (int)SystemParameters.PrimaryScreenWidth,
                            bottom = (int)SystemParameters.PrimaryScreenHeight
                        };
                    }
                }

                // https://stackoverflow.com/questions/1918877/how-can-i-get-the-dpi-in-wpf
                var dpiAtPoint = DpiUtilities.GetDpiForNearestMonitor(caretRect.right, caretRect.bottom);
                popup.Left = caretRect.right * DpiUtilities.DefaultDpiX / dpiAtPoint;
                popup.Top = caretRect.bottom * DpiUtilities.DefaultDpiY / dpiAtPoint;
                WindowPositionHelper.ShiftWindowToScreen(popup);
                //popup.ForegroundHWND = hwndFocus;
                popup.Show();
                var popuHandle = new WindowInteropHelper(popup).EnsureHandle();
                WinApiProvider.SetForegroundWindow(popuHandle);
            }
        }

        private static WinApiProvider.RECT GetAccessibleCaretRect(IntPtr hwnd)
        {
            var guid = typeof(IAccessible).GUID;
            object accessibleObject = null;
            var retVal = WinApiProvider.AccessibleObjectFromWindow(hwnd, WinApiProvider.OBJID_CARET, ref guid, ref accessibleObject);
            var accessible = accessibleObject as IAccessible;
            accessible.accLocation(out int left, out int top, out int width, out int height, WinApiProvider.CHILDID_SELF);
            return new WinApiProvider.RECT() { bottom = top + height, left = left, right = left + width, top = top };
        }

        private static WinApiProvider.RECT GetWinApiCaretRect(IntPtr hwnd)
        {
            // Try WinAPI
            uint idAttach = 0;
            uint curThreadId = 0;
            WinApiProvider.POINT caretPoint;
            try
            {
                idAttach = WinApiProvider.GetWindowThreadProcessId(hwnd, out uint id);
                curThreadId = WinApiProvider.GetCurrentThreadId();
                // To attach to current thread
                var sa = WinApiProvider.AttachThreadInput(idAttach, curThreadId, true);
                var caretPos = WinApiProvider.GetCaretPos(out caretPoint);
                WinApiProvider.ClientToScreen(hwnd, ref caretPoint);
            }
            finally
            {
                // To dettach from current thread
                var sd = WinApiProvider.AttachThreadInput(idAttach, curThreadId, false);
            }

            return new WinApiProvider.RECT()
            {
                left = caretPoint.X,
                top = caretPoint.Y,
                bottom = caretPoint.Y + 20,
                right = caretPoint.X + 1
            };
        }

        private static bool RectValid(WinApiProvider.RECT rect)
        {
            return rect.left != 0 && rect.top != 0 && rect.right != 0 && rect.bottom != 0;
        }
    }


    internal static class WinApiProvider
    {
        public const int IDI_APPLICATION = 0x7F00;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("oleacc.dll")]
        internal static extern int AccessibleObjectFromWindow(
         IntPtr hwnd,
         uint id,
         ref Guid iid,
         [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object ppvObject);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCaretPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        public const uint SWP_NOZORDER = 0x0004;

        public const int CHILDID_SELF = 0;
        public const uint OBJID_CARET = 0xFFFFFFF8;

        [StructLayout(LayoutKind.Sequential)]
        public struct GUITHREADINFO
        {
            public int cbSize;
            public GuiThreadInfoFlags flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public Rectangle rcCaret;
        }

        [Flags]
        public enum GuiThreadInfoFlags
        {
            GUI_CARETBLINKING = 0x00000001,
            GUI_INMENUMODE = 0x00000004,
            GUI_INMOVESIZE = 0x00000002,
            GUI_POPUPMENUMODE = 0x00000010,
            GUI_SYSTEMMENUMODE = 0x00000008
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }
    }

    public static class WindowPositionHelper
    {
        /// <summary>
        /// Shifts window to nearest screen if it's out of it.
        /// </summary>
        /// <param name="window">Target window.</param>
        public static void ShiftWindowToScreen(Window window)
        {
            var windowPoint = new System.Drawing.Point((int)window.Left, (int)window.Top);
            Screen activeScreen = Screen.FromPoint(windowPoint);

            var windowRight = window.Left + window.Width;
            var screenRight = activeScreen.WorkingArea.X + activeScreen.WorkingArea.Width;
            if (windowRight > screenRight)
            {
                window.Left = screenRight - window.Width;
            }

            var windowBottom = window.Top + window.Height;
            var screenBottom = activeScreen.WorkingArea.Y + activeScreen.WorkingArea.Height;
            if (windowBottom > screenBottom)
            {
                window.Top = screenBottom - window.Height;
            }
        }
    }

    // https://stackoverflow.com/questions/1918877/how-can-i-get-the-dpi-in-wpf
    public static class DpiUtilities
    {
        public const float DefaultDpiX = 96;
        public const float DefaultDpiY = 96;

        // you should always use this one and it will fallback if necessary
        // https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getdpiforwindow
        public static int GetDpiForWindow(IntPtr hwnd)
        {
            var h = LoadLibrary("user32.dll");
            var ptr = GetProcAddress(h, "GetDpiForWindow"); // Windows 10 1607
            if (ptr == IntPtr.Zero)
                return GetDpiForNearestMonitor(hwnd);

            return Marshal.GetDelegateForFunctionPointer<GetDpiForWindowFn>(ptr)(hwnd);
        }

        public static int GetDpiForNearestMonitor(IntPtr hwnd) => GetDpiForMonitor(GetNearestMonitorFromWindow(hwnd));
        public static int GetDpiForNearestMonitor(int x, int y) => GetDpiForMonitor(GetNearestMonitorFromPoint(x, y));
        public static int GetDpiForMonitor(IntPtr monitor, MonitorDpiType type = MonitorDpiType.Effective)
        {
            var h = LoadLibrary("shcore.dll");
            var ptr = GetProcAddress(h, "GetDpiForMonitor"); // Windows 8.1
            if (ptr == IntPtr.Zero)
                return GetDpiForDesktop();

            int hr = Marshal.GetDelegateForFunctionPointer<GetDpiForMonitorFn>(ptr)(monitor, type, out int x, out int y);
            if (hr < 0)
                return GetDpiForDesktop();

            return x;
        }

        public static int GetDpiForDesktop()
        {
            int hr = D2D1CreateFactory(D2D1_FACTORY_TYPE.D2D1_FACTORY_TYPE_SINGLE_THREADED, typeof(ID2D1Factory).GUID, IntPtr.Zero, out ID2D1Factory factory);
            if (hr < 0)
                return 96; // we really hit the ground, don't know what to do next!

            factory.GetDesktopDpi(out float x, out float y); // Windows 7
            Marshal.ReleaseComObject(factory);
            return (int)x;
        }

        public static IntPtr GetDesktopMonitor() => GetNearestMonitorFromWindow(GetDesktopWindow());
        public static IntPtr GetShellMonitor() => GetNearestMonitorFromWindow(GetShellWindow());
        public static IntPtr GetNearestMonitorFromWindow(IntPtr hwnd) => MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        public static IntPtr GetNearestMonitorFromPoint(int x, int y) => MonitorFromPoint(new POINT { x = x, y = y }, MONITOR_DEFAULTTONEAREST);

        private delegate int GetDpiForWindowFn(IntPtr hwnd);
        private delegate int GetDpiForMonitorFn(IntPtr hmonitor, MonitorDpiType dpiType, out int dpiX, out int dpiY);

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("user32")]
        private static extern IntPtr MonitorFromPoint(POINT pt, int flags);

        [DllImport("user32")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32")]
        private static extern IntPtr GetShellWindow();

        [StructLayout(LayoutKind.Sequential)]
        private partial struct POINT
        {
            public int x;
            public int y;
        }

        [DllImport("d2d1")]
        private static extern int D2D1CreateFactory(D2D1_FACTORY_TYPE factoryType, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, IntPtr pFactoryOptions, out ID2D1Factory ppIFactory);

        private enum D2D1_FACTORY_TYPE
        {
            D2D1_FACTORY_TYPE_SINGLE_THREADED = 0,
            D2D1_FACTORY_TYPE_MULTI_THREADED = 1,
        }

        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("06152247-6f50-465a-9245-118bfd3b6007")]
        private interface ID2D1Factory
        {
            int ReloadSystemMetrics();

            [PreserveSig]
            void GetDesktopDpi(out float dpiX, out float dpiY);

            // the rest is not implemented as we don't need it
        }
    }

    public enum MonitorDpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}
