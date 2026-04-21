using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// Avalonia-host-specific clipboard plugin. Uses the cross-platform
/// <see cref="ClipboardSnippetProvider"/> that reads the current clipboard via
/// Avalonia's <c>IClipboard</c>. Clipboard history is not available on this host.
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
