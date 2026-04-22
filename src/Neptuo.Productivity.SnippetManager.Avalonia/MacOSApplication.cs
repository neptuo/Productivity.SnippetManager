using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace Neptuo.Productivity.SnippetManager;

internal readonly record struct FrontmostApplication(int ProcessId, string? Name, string? BundleIdentifier)
{
    public string DescribeForLog()
    {
        string name = FormatLogValue(Name);
        string bundle = FormatLogValue(BundleIdentifier);
        return $"pid={ProcessId}, name={name}, bundle={bundle}";
    }

    private static string FormatLogValue(string? value)
        => string.IsNullOrEmpty(value) ? "<unknown>" : QuoteForLog(value);

    private static string QuoteForLog(string value)
    {
        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (char c in value)
        {
            switch (c)
            {
                case '\\': builder.Append("\\\\"); break;
                case '"': builder.Append("\\\""); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                default:
                    if (c < 0x20)
                        builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else
                        builder.Append(c);
                    break;
            }
        }
        builder.Append('"');
        return builder.ToString();
    }
}

internal static class MacOSApplication
{
    public static FrontmostApplication? GetFrontmostApplication()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        try
        {
            IntPtr workspaceClass = ObjC.objc_getClass("NSWorkspace");
            if (workspaceClass == IntPtr.Zero)
            {
                DiagnosticsLog.Error("Unable to resolve the NSWorkspace class via the Objective-C runtime.");
                return null;
            }

            IntPtr sharedWorkspace = ObjC.IntPtr_objc_msgSend(workspaceClass, ObjC.Sel_sharedWorkspace);
            if (sharedWorkspace == IntPtr.Zero)
            {
                DiagnosticsLog.Error("Unable to resolve [NSWorkspace sharedWorkspace].");
                return null;
            }

            IntPtr frontApp = ObjC.IntPtr_objc_msgSend(sharedWorkspace, ObjC.Sel_frontmostApplication);
            if (frontApp == IntPtr.Zero)
            {
                DiagnosticsLog.Error("Unable to resolve the frontmost macOS application (NSWorkspace returned nil).");
                return null;
            }

            int processId = ObjC.Int_objc_msgSend(frontApp, ObjC.Sel_processIdentifier);
            string? name = ObjC.ReadNSString(ObjC.IntPtr_objc_msgSend(frontApp, ObjC.Sel_localizedName));
            string? bundle = ObjC.ReadNSString(ObjC.IntPtr_objc_msgSend(frontApp, ObjC.Sel_bundleIdentifier));

            if (processId <= 0)
            {
                DiagnosticsLog.Error($"Frontmost macOS application returned a non-positive PID ({processId}).");
                return null;
            }

            var app = new FrontmostApplication(processId, name, bundle);
            DiagnosticsLog.Info($"Resolved the frontmost macOS application: {app.DescribeForLog()}.");
            return app;
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Unable to resolve the frontmost macOS application via NSWorkspace.", ex);
            return null;
        }
    }

    public static void ActivateCurrentProcess()
    {
        DiagnosticsLog.Info($"Requesting activation for the current macOS process {Environment.ProcessId}.");
        ActivateProcess(Environment.ProcessId);
    }

    public static void ActivateProcess(int processId, string? name = null)
    {
        if (!OperatingSystem.IsMacOS() || processId <= 0)
            return;

        string suffix = string.IsNullOrEmpty(name) ? string.Empty : $" (name='{name}')";
        DiagnosticsLog.Info($"Requesting macOS activation for process {processId}{suffix}.");

        try
        {
            IntPtr runningAppClass = ObjC.objc_getClass("NSRunningApplication");
            if (runningAppClass == IntPtr.Zero)
            {
                DiagnosticsLog.Error("Unable to resolve the NSRunningApplication class.");
                return;
            }

            IntPtr app = ObjC.IntPtr_IntPtr_objc_msgSend(runningAppClass, ObjC.Sel_runningApplicationWithProcessIdentifier, new IntPtr(processId));
            if (app == IntPtr.Zero)
            {
                DiagnosticsLog.Error($"No NSRunningApplication found for PID {processId}.");
                return;
            }

            // NSApplicationActivateIgnoringOtherApps = 1 << 1
            const ulong NSApplicationActivateIgnoringOtherApps = 1UL << 1;
            bool ok = ObjC.Bool_ULong_objc_msgSend(app, ObjC.Sel_activateWithOptions, NSApplicationActivateIgnoringOtherApps);
            if (!ok)
                DiagnosticsLog.Error($"NSRunningApplication activateWithOptions: returned NO for PID {processId}.");
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error($"Unable to activate macOS process {processId} via NSRunningApplication.", ex);
        }
    }

    public static void SendPasteShortcut()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        DiagnosticsLog.Info("Sending macOS paste shortcut via CGEvent.");

        try
        {
            const ushort kVK_ANSI_V = 0x09;
            const ulong kCGEventFlagMaskCommand = 0x00100000;
            const uint kCGHIDEventTap = 0;

            IntPtr source = CoreGraphics.CGEventSourceCreate(1 /* kCGEventSourceStateHIDSystemState */);
            try
            {
                IntPtr keyDown = CoreGraphics.CGEventCreateKeyboardEvent(source, kVK_ANSI_V, true);
                IntPtr keyUp = CoreGraphics.CGEventCreateKeyboardEvent(source, kVK_ANSI_V, false);
                try
                {
                    if (keyDown == IntPtr.Zero || keyUp == IntPtr.Zero)
                    {
                        DiagnosticsLog.Error("CGEventCreateKeyboardEvent returned null; cannot send paste shortcut.");
                        return;
                    }

                    CoreGraphics.CGEventSetFlags(keyDown, kCGEventFlagMaskCommand);
                    CoreGraphics.CGEventSetFlags(keyUp, kCGEventFlagMaskCommand);
                    CoreGraphics.CGEventPost(kCGHIDEventTap, keyDown);
                    CoreGraphics.CGEventPost(kCGHIDEventTap, keyUp);
                }
                finally
                {
                    if (keyDown != IntPtr.Zero) CoreGraphics.CFRelease(keyDown);
                    if (keyUp != IntPtr.Zero) CoreGraphics.CFRelease(keyUp);
                }
            }
            finally
            {
                if (source != IntPtr.Zero) CoreGraphics.CFRelease(source);
            }
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Unable to send the macOS paste shortcut via CGEvent.", ex);
        }
    }

    private static class CoreGraphics
    {
        private const string Framework = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        private const string CoreFoundation = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        [DllImport(Framework)]
        public static extern IntPtr CGEventSourceCreate(int stateID);

        [DllImport(Framework)]
        public static extern IntPtr CGEventCreateKeyboardEvent(IntPtr source, ushort virtualKey, [MarshalAs(UnmanagedType.I1)] bool keyDown);

        [DllImport(Framework)]
        public static extern void CGEventSetFlags(IntPtr @event, ulong flags);

        [DllImport(Framework)]
        public static extern void CGEventPost(uint tap, IntPtr @event);

        [DllImport(CoreFoundation)]
        public static extern void CFRelease(IntPtr cf);
    }

    private static class ObjC
    {
        private const string LibObjC = "/usr/lib/libobjc.dylib";
        private const string LibSystem = "/usr/lib/libSystem.dylib";
        private const string AppKitPath = "/System/Library/Frameworks/AppKit.framework/AppKit";

        // Ensure AppKit is loaded so NSWorkspace is available. Idempotent; a no-op when Avalonia has already linked it.
        private static readonly IntPtr AppKitHandle = dlopen(AppKitPath, 2 /* RTLD_NOW */);

        public static readonly IntPtr Sel_sharedWorkspace = sel_registerName("sharedWorkspace");
        public static readonly IntPtr Sel_frontmostApplication = sel_registerName("frontmostApplication");
        public static readonly IntPtr Sel_processIdentifier = sel_registerName("processIdentifier");
        public static readonly IntPtr Sel_localizedName = sel_registerName("localizedName");
        public static readonly IntPtr Sel_bundleIdentifier = sel_registerName("bundleIdentifier");
        public static readonly IntPtr Sel_UTF8String = sel_registerName("UTF8String");
        public static readonly IntPtr Sel_runningApplicationWithProcessIdentifier = sel_registerName("runningApplicationWithProcessIdentifier:");
        public static readonly IntPtr Sel_activateWithOptions = sel_registerName("activateWithOptions:");

        [DllImport(LibSystem, CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport(LibObjC, CharSet = CharSet.Ansi)]
        public static extern IntPtr objc_getClass(string name);

        [DllImport(LibObjC, CharSet = CharSet.Ansi)]
        public static extern IntPtr sel_registerName(string name);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern int Int_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool Bool_ULong_objc_msgSend(IntPtr receiver, IntPtr selector, ulong arg);

        public static string? ReadNSString(IntPtr nsString)
        {
            if (nsString == IntPtr.Zero)
                return null;

            IntPtr utf8 = IntPtr_objc_msgSend(nsString, Sel_UTF8String);
            if (utf8 == IntPtr.Zero)
                return null;

            string? value = Marshal.PtrToStringUTF8(utf8);
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
