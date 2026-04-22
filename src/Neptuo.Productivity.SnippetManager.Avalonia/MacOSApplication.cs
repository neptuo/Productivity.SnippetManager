using System.Diagnostics;
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
        RunAppleScript($"""
            tell application "System Events"
                set frontmost of first application process whose unix id is {processId} to true
            end tell
            """);
    }

    public static void SendPasteShortcut()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        DiagnosticsLog.Info("Sending macOS paste shortcut via AppleScript.");
        RunAppleScript("""
            tell application "System Events"
                keystroke "v" using command down
            end tell
            """);
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

        [DllImport(LibSystem, CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string path, int mode);

        [DllImport(LibObjC, CharSet = CharSet.Ansi)]
        public static extern IntPtr objc_getClass(string name);

        [DllImport(LibObjC, CharSet = CharSet.Ansi)]
        public static extern IntPtr sel_registerName(string name);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern IntPtr IntPtr_objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport(LibObjC, EntryPoint = "objc_msgSend")]
        public static extern int Int_objc_msgSend(IntPtr receiver, IntPtr selector);

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

    private static string? RunAppleScript(string script)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "osascript",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            process.StartInfo.ArgumentList.Add("-e");
            process.StartInfo.ArgumentList.Add(script);

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                DiagnosticsLog.Error($"AppleScript exited with code {process.ExitCode}: {error}");
                return null;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Unable to run AppleScript.", ex);
            return null;
        }
    }
}
