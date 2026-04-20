using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("Guid", Priority = 20)]
public sealed class GuidPlugin : ISnippetManagerPlugin
{
    public const string Key = "Guid";

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddConfigChangeTracking<ProviderConfiguration>(
            Key,
            _ => new GuidSnippetProvider(),
            isNullConfigurationEnabled: true);
}
