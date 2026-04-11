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
            return processId;

        Debug.WriteLine($"Unable to parse the frontmost macOS application PID from '{output}'.");
        return null;
    }

    public static void ActivateCurrentProcess()
        => ActivateProcess(Environment.ProcessId);

    public static void ActivateProcess(int processId)
    {
        if (!OperatingSystem.IsMacOS() || processId <= 0)
            return;

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
                Debug.WriteLine($"AppleScript exited with code {process.ExitCode}: {error}");
                return null;
            }

            return output.Trim();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Unable to run AppleScript: {ex}");
            return null;
        }
    }
}
