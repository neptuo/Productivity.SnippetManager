namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Host-provided navigation surface plugins may use when contributing tray
/// menu items (or other UI extension points). The host exports an instance
/// via MEF; plugins import it with <c>[Import] INavigator</c>.
/// </summary>
public interface INavigator
{
    /// <summary>
    /// Opens the snippet suggestion window.
    /// </summary>
    void Open();

    /// <summary>
    /// Opens the configuration file.
    /// </summary>
    void OpenConfiguration();

    /// <summary>
    /// Opens a file using whatever mechanism the host considers
    /// appropriate (e.g. <c>Process.Start</c> with shell execute).
    /// </summary>
    void OpenFile(string path);
}
