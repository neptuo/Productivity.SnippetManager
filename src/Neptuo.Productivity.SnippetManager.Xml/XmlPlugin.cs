using System.ComponentModel.Composition;
using System.IO;
using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

[SnippetManagerPlugin("Xml", Priority = 30)]
[Export(typeof(ITrayMenuContributor))]
public sealed class XmlPlugin : ISnippetManagerPlugin, ITrayMenuContributor
{
    public const string Key = "Xml";

    private XmlConfiguration? currentConfiguration;
    private XmlSnippetProvider? currentProvider;

    [Import]
    internal INavigator Navigator { get; set; } = default!;

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
        var filePaths = GetResolvedFilePaths();
        string primary = filePaths[0];
        Action openPrimary = () => OpenXmlFile(primary);

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
                    sub.AddItem(label, () => OpenXmlFile(captured));
                }
            });
        }
    }

    private void OpenXmlFile(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, """
                <?xml version="1.0" encoding="utf-8" ?>
                <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
                  <Snippet Title="Greet" Text="Hello, World!" />
                  <Snippet Title="Weather Forecast" Priority="High">
                <![CDATA[Prague 22,
                London 18,
                New York 25]]></Snippet>
                </Snippets>
                """);
        }

        Navigator.OpenFile(path);
    }

    private IReadOnlyList<string> GetResolvedFilePaths()
    {
        if (currentProvider is not null && currentProvider.ResolvedFilePaths.Count > 0)
            return currentProvider.ResolvedFilePaths;

        string fallback = (currentConfiguration ?? XmlConfiguration.Example).GetFilePathOrDefault();
        return new[] { fallback };
    }
}
