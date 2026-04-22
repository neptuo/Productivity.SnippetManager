using System.Runtime.InteropServices;

namespace Neptuo.Productivity.SnippetManager;

internal static class WindowsApplication
{
    private const int VK_CONTROL = 0x11;

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public static bool IsCtrlKeyPressed()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        try
        {
            // High-order bit is set if the key is currently down.
            return (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Unable to query Windows modifier key state.", ex);
            return false;
        }
    }
}
