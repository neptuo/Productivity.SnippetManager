using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("Xml")]
public sealed class XmlPlugin : ISnippetManagerPlugin
{
    public const string Key = "Xml";

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddConfigChangeTracking<XmlConfiguration>(
            Key,
            c => new XmlSnippetProvider(c),
            isNullConfigurationEnabled: true);
}
