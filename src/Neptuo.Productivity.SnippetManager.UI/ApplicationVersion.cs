using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Windows.ApplicationModel;

namespace Neptuo.Productivity.SnippetManager;

public static class ApplicationVersion
{
    public static string GetDisplayString()
    {
        if (Uwp.Is())
            return GetVersionFromPackage();

        return GetVersionFromAssemblyAttribute();
    }

    private static string GetVersionFromPackage()
    {
        var version = Package.Current.Id.Version;
        string versionText = $"v{version.Major}.{version.Minor}.{version.Build}";
        if (version.Revision > 0)
            versionText += $".{version.Revision}";

        return versionText;
    }

    private static string GetVersionFromAssemblyAttribute()
    {
        string? version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return String.Format("v{0}", version);
    }
}

public static class Uwp
{
    private const long NoPackageError = 15700;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, StringBuilder packageFullName);

    private static bool? isContainerized;

    public static bool Is()
    {
        if (isContainerized == null)
        {
            isContainerized = false;

            if (Environment.OSVersion.Version.Major >= 10)
            {
                int length = 0;
                int result = GetCurrentPackageFullName(ref length, new StringBuilder(0));
                isContainerized = result != NoPackageError;
            }
        }

        return isContainerized.Value;
    }
}