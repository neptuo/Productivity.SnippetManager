using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// Avalonia-host implementation of <see cref="ITrayHostServices"/>. The live
/// <see cref="Navigator"/> is swapped in whenever the app (re)composes, so
/// plugin-contributed tray menu items always call into the current
/// navigator instance.
/// </summary>
internal sealed class HostServices : ITrayHostServices
{
    public Navigator? Navigator { get; set; }

    public void OpenFile(string path)
        => Navigator?.OpenXmlSnippets(path);
}
