namespace Neptuo.Productivity.SnippetManager.Variables;

public class VariablesConfiguration : Dictionary<string, string>
{
    public static VariablesConfiguration Example
        => new()
        {
            ["ShellExt"] = OperatingSystem.IsWindows() ? "cmd" : "sh"
        };
}
