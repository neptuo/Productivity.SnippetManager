namespace Neptuo.Productivity.SnippetManager;

public class GeneralConfiguration
{
    public string? HotKey { get; set; }

    public static GeneralConfiguration Example = new GeneralConfiguration()
    {
        HotKey = "Control+Shift+V"
    };
}
