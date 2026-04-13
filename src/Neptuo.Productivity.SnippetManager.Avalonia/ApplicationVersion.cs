using System.Reflection;

namespace Neptuo.Productivity.SnippetManager;

public static class ApplicationVersion
{
    public static string GetDisplayString()
    {
        string? version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(version))
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        return $"v{version}";
    }
}
