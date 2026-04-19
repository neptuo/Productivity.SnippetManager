using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class Win32
{
    public delegate int RecoveryDelegate(IntPtr parameterData);

    [DllImport("kernel32.dll")]
    public static extern int RegisterApplicationRecoveryCallback(RecoveryDelegate recoveryCallback, IntPtr parameter, uint pingInterval, uint flags);

    [DllImport("kernel32.dll")]
    public static extern int ApplicationRecoveryInProgress(out bool canceled);

    [DllImport("kernel32.dll")]
    public static extern void ApplicationRecoveryFinished(bool success);

    [DllImport("kernel32.dll")]
    public static extern int UnregisterApplicationRecoveryCallback();

    [DllImport("kernel32.dll")]
    public static extern int RegisterApplicationRestart([MarshalAs(UnmanagedType.LPWStr)] string commandLineArgs, RestartRestrictions flags);

    [DllImport("kernel32.dll")]
    public static extern int UnregisterApplicationRestart();

    [Flags]
    public enum RestartRestrictions
    {
        None = 0,
        NotOnCrash = 1,
        NotOnHang = 2,
        NotOnPatch = 4,
        NotOnReboot = 8,
    }
}
