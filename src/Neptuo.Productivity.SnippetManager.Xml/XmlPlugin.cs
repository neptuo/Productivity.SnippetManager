using System.ComponentModel.Composition;
using System.IO;
using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("Xml")]
[Export(typeof(ITrayMenuContributor))]
public sealed class XmlPlugin : ISnippetManagerPlugin, ITrayMenuContributor
{
    public const string Key = "Xml";

    private XmlConfiguration? currentConfiguration;
    private XmlSnippetProvider? currentProvider;

    [Import(AllowDefault = true)]
    internal ITrayHostServices? HostServices { get; set; }

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddConfigChangeTracking<XmlConfiguration>(
            Key,
            c =>
            {
                currentConfiguration = c;
                return currentProvider = new XmlSnippetProvider(c);
            },
            isNullConfigurationEnabled: true);

    public void Contribute(ITrayMenuBuilder menu)
    {
        if (HostServices is null)
            return;

        var filePaths = GetResolvedFilePaths();
        if (filePaths.Count == 0)
            return;

        string primary = filePaths[0];
        Action openPrimary = () => HostServices.OpenFile(primary);

        if (filePaths.Count == 1)
        {
            menu.AddItem("XML snippets", openPrimary);
        }
        else
        {
            menu.AddSubMenu("XML snippets", openPrimary, sub =>
            {
                foreach (string path in filePaths)
                {
                    string label = Path.GetFileName(path);
                    string captured = path;
                    sub.AddItem(label, () => HostServices.OpenFile(captured));
                }
            });
        }
    }

    private IReadOnlyList<string> GetResolvedFilePaths()
    {
        if (currentProvider is not null && currentProvider.ResolvedFilePaths.Count > 0)
            return currentProvider.ResolvedFilePaths;

        string fallback = (currentConfiguration ?? XmlConfiguration.Example).GetFilePathOrDefault();
        return new[] { fallback };
    }
}
