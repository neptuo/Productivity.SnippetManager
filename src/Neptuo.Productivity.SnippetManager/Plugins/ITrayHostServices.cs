namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Host-provided services that plugins may need when contributing tray
/// menu items (and in the future, other UI extension points). Hosts export
/// an implementation via MEF; plugins import it with
/// <c>[Import(typeof(ITrayHostServices), AllowDefault = true)]</c>.
/// </summary>
public interface ITrayHostServices
{
    /// <summary>
    /// Opens a snippet-related file (e.g. the XML snippets file) using
    /// whatever mechanism the host considers appropriate: on WPF this opens
    /// the file in the user's default editor and may prompt the user to
    /// create it if missing; Avalonia hosts follow an equivalent
    /// platform-specific flow.
    /// </summary>
    void OpenFile(string path);
}
