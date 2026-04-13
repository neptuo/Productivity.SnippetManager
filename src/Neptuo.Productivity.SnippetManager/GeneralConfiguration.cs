namespace Neptuo.Productivity.SnippetManager;

public class GeneralConfiguration
{
    public const string WindowsDefaultHotKey = "Control+Shift+V";
    public const string MacDefaultHotKey = "Command+Shift+V";

    public static string DefaultHotKey
        => OperatingSystem.IsMacOS() ? MacDefaultHotKey : WindowsDefaultHotKey;

    public string? HotKey { get; set; }

    public static GeneralConfiguration Example
        => new()
        {
            HotKey = DefaultHotKey
        };
}
