using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// WPF-host-specific clipboard plugin. Uses the WPF <see cref="ClipboardSnippetProvider"/>
/// that reads the current clipboard via <c>System.Windows.Forms.Clipboard</c> and (when
/// available) the WinRT clipboard history API.
/// </summary>
[SnippetManagerPlugin("Clipboard", Priority = 10)]
public sealed class ClipboardPlugin : ISnippetManagerPlugin
{
    public const string Key = "Clipboard";

    public void Register(ISnippetProviderRegistry registry)
        => registry.AddConfigChangeTracking<ProviderConfiguration>(
            Key,
            _ => new ClipboardSnippetProvider(),
            isNullConfigurationEnabled: true);
}
