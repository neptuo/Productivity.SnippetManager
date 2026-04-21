using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("Snippets", Priority = 50)]
public sealed class InlinePlugin : ISnippetManagerPlugin
{
    public const string Key = "Snippets";

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddNotNullConfiguration<InlineSnippetConfiguration>(
            Key,
            c => new InlineSnippetProvider(c));
}
