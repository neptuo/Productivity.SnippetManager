using System.Diagnostics;
using System.Globalization;

namespace Neptuo.Productivity.SnippetManager;

internal static class MacOSApplication
{
    public static int? GetFrontmostApplicationProcessId()
    {
        if (!OperatingSystem.IsMacOS())
            return null;

        string? output = RunAppleScript("""
            tell application "System Events"
                unix id of first application process whose frontmost is true
            end tell
            """);

        if (int.TryParse(output, NumberStyles.Integer, CultureInfo.InvariantCulture, out int processId))
        {
            DiagnosticsLog.Info($"Resolved the frontmost macOS application PID to {processId}.");
            return processId;
        }

        DiagnosticsLog.Error($"Unable to parse the frontmost macOS application PID from '{output}'.");
        return null;
    }

    public static void ActivateCurrentProcess()
    {
        DiagnosticsLog.Info($"Requesting activation for the current macOS process {Environment.ProcessId}.");
        ActivateProcess(Environment.ProcessId);
    }

    public static void ActivateProcess(int processId)
    {
        if (!OperatingSystem.IsMacOS() || processId <= 0)
            return;

        DiagnosticsLog.Info($"Requesting macOS activation for process {processId}.");
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
