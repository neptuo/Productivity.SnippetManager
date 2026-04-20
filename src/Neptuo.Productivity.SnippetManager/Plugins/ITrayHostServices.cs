namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Host-provided services that plugins may need when contributing tray
/// menu items (and in the future, other UI extension points). The active
/// instance is passed into <see cref="ITrayMenuContributor.Contribute"/>
/// on every menu open, so contributors always see the current host state.
/// </summary>
public interface ITrayHostServices
{
    /// <summary>
    /// Opens a file using whatever mechanism the host considers
    /// appropriate (e.g. <c>Process.Start</c> with shell execute).
    /// </summary>
    void OpenFile(string path);
}
