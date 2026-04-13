namespace Neptuo.Productivity.SnippetManager;

public class GeneralConfiguration
{
    public const string NonMacDefaultHotKey = "Control+Shift+V";
    public const string MacDefaultHotKey = "Command+Shift+V";

    public static string DefaultHotKey
        => OperatingSystem.IsMacOS() ? MacDefaultHotKey : NonMacDefaultHotKey;

    public string? HotKey { get; set; }

    public static GeneralConfiguration Example
        => new()
        {
            HotKey = DefaultHotKey
        };
}
