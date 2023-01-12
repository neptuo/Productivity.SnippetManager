using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows;
using System.Drawing;
using UIAutomationClient;

namespace Neptuo.Productivity.SnippetManager.Views.Controls;

public static class CaretPosition
{
    public static Rectangle? Find()
    {
        var info = new Win32.GUITHREADINFO();
        info.cbSize = Marshal.SizeOf(info);
        if (Win32.GetGUIThreadInfo(0, ref info))
        {
            var hwndFocus = info.hwndFocus;

            Rectangle? caretRect = null;
            caretRect = FindUiAutomationCaretPosition(hwndFocus);
            if (caretRect == null)
                caretRect = FindWinApiCaretPosition(hwndFocus);

            return caretRect;
        }

        return null;
    }

    private static Rectangle? FindUiAutomationCaretPosition(IntPtr hwnd)
    {
        var accessibleGuid = typeof(IAccessible).GUID;
        object? accessibleObject = null;
        _ = Win32.AccessibleObjectFromWindow(hwnd, Win32.OBJID_CARET, ref accessibleGuid, ref accessibleObject);
        if (accessibleObject is IAccessible accessible)
        {
            accessible.accLocation(out int left, out int top, out int width, out int height, Win32.CHILDID_SELF);
            if (left != 0 && top != 0 && width != 0 && height != 0)
                return new Rectangle(left, top, width, height);
        }

        CUIAutomation automation = new CUIAutomation();
        IUIAutomationElement element = automation.GetFocusedElement();

        //UIA_PatternIds.UIA_TextPattern2Id
        var pattern2 = (IUIAutomationTextPattern2)element.GetCurrentPattern(10024);
        if (pattern2 != null)
        {
            var documentRange = pattern2.DocumentRange;
            var caretRange = pattern2.GetCaretRange(out _);
            if (caretRange != null)
            {
                var bounds = caretRange.GetBoundingRectangles();

                if (bounds.Length == 4 && bounds is double[] coords)
                    return new Rectangle((int)coords[0], (int)coords[1], (int)coords[2], (int)coords[3]);
            }
        }


        var pattern1 = (IUIAutomationTextPattern)element.GetCurrentPattern(10014);
        if (pattern1 != null)
        {
            var selectons = pattern1.GetSelection();
            if (selectons.Length > 0)
            {
                var selection = selectons.GetElement(0);
                var bounds = selection.GetBoundingRectangles();

                if (bounds.Length == 4 && bounds is double[] coords)
                    return new Rectangle((int)coords[0], (int)coords[1], (int)coords[2], (int)coords[3]);
            }
        }

        _ = element.GetClickablePoint(out var point);
        if (point.x != 0 && point.y != 0)
            return new Rectangle(point.x, point.y, 1, 20);

        return null;
    }

    private static Rectangle? FindWinApiCaretPosition(IntPtr hwnd)
    {
        uint idAttach = 0;
        uint curThreadId = 0;
        Win32.POINT caretPoint;
        try
        {
            idAttach = Win32.GetWindowThreadProcessId(hwnd, out uint id);
            curThreadId = Win32.GetCurrentThreadId();

            Win32.AttachThreadInput(idAttach, curThreadId, true);
            Win32.GetCaretPos(out caretPoint);

            // Because when there isn't a way to find caret, it return 7,11
            if (caretPoint.X < 20 || caretPoint.Y < 20)
                return null;

            Win32.ClientToScreen(hwnd, ref caretPoint);
        }
        finally
        {
            Win32.AttachThreadInput(idAttach, curThreadId, false);
        }

        if (caretPoint.X == 0 && caretPoint.Y == 0)
            return null;

        return new Rectangle(
            caretPoint.X,
            caretPoint.Y,
            1,
            20
        );
    }

    class Win32
    {
        public const int IDI_APPLICATION = 0x7F00;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("oleacc.dll")]
        internal static extern int AccessibleObjectFromWindow(IntPtr hwnd, uint id, ref Guid iid, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object? ppvObject);

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
        public const uint OBJID_CURSOR = 0xFFFFFFF7;

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
        public struct POINT
        {
            public int X;
            public int Y;
        }
    }
}
