using System.Diagnostics;
using System.Globalization;

namespace Neptuo.Productivity.SnippetManager;

internal readonly record struct FrontmostApplication(int ProcessId, string? Name, string? BundleIdentifier)
{
    public string DescribeForLog()
    {
        string name = string.IsNullOrEmpty(Name) ? "<unknown>" : Name;
        string bundle = string.IsNullOrEmpty(BundleIdentifier) ? "<unknown>" : BundleIdentifier;
        return $"pid={ProcessId}, name='{name}', bundle='{bundle}'";
    }
}

internal static class MacOSApplication
{
    public static FrontmostApplication? GetFrontmostApplication()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        string? output = RunAppleScript("""
            tell application "System Events"
                set p to first application process whose frontmost is true
                set n to ""
                try
                    set n to name of p
                end try
                set b to ""
                try
                    set b to bundle identifier of p
                end try
                return ((unix id of p) as string) & tab & n & tab & b
            end tell
            """);

        if (string.IsNullOrEmpty(output))
        {
            DiagnosticsLog.Error("Unable to resolve the frontmost macOS application (empty AppleScript output).");
            return null;
        }

        string[] parts = output.Split('\t');
        if (parts.Length == 0 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId))
        {
            DiagnosticsLog.Error($"Unable to parse the frontmost macOS application info from '{output}'.");
            return null;
        }

        string? name = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) ? parts[1] : null;
        string? bundle = parts.Length > 2 && !string.IsNullOrEmpty(parts[2]) ? parts[2] : null;

        var app = new FrontmostApplication(processId, name, bundle);
        DiagnosticsLog.Info($"Resolved the frontmost macOS application: {app.DescribeForLog()}.");
        return app;
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
