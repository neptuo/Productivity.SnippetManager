namespace Neptuo.Productivity.SnippetManager;

public class InlineSnippetConfiguration : Dictionary<string, string>, IProviderConfiguration<InlineSnippetConfiguration>
{
    public static InlineSnippetConfiguration Example => new()
    {
        ["Hello"] = "Hello, World!"
    };
}
