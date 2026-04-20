using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("GitHub", Priority = 40)]
public sealed class GitHubPlugin : ISnippetManagerPlugin
{
    public const string Key = "GitHub";

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddConfigChangeTracking<GitHubConfiguration>(
            Key,
            c => new GitHubSnippetProvider(c));
}
