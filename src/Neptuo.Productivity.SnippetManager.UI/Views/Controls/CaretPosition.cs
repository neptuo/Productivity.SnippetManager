﻿using System;
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

            var caretRect = FindAccessibleCaretPosition(hwndFocus);
            if (caretRect == null)
                caretRect = FindWinApiCaretPosition(hwndFocus);

            return caretRect;
        }

        return null;
    }

    private static Rectangle? FindAccessibleCaretPosition(IntPtr hwnd)
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
            caretPoint.Y + 20,
            caretPoint.X + 1
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
