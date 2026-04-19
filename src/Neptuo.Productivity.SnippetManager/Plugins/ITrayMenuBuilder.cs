namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Host-agnostic tray-menu construction surface passed to
/// <see cref="ITrayMenuContributor"/> implementations. Each host
/// (WPF NotifyIcon, Avalonia TrayIcon, ...) implements this interface over
/// its native menu types so plugins never reference UI-toolkit types.
/// </summary>
public interface ITrayMenuBuilder
{
    /// <summary>
    /// Adds a clickable leaf item.
    /// </summary>
    void AddItem(string label, Action onClick);

    /// <summary>
    /// Adds a submenu whose children are populated by
    /// <paramref name="buildChildren"/>. If <paramref name="onClick"/> is not
    /// null the submenu's header is itself clickable (WPF honors this; some
    /// Avalonia backends ignore it for non-empty submenus).
    /// </summary>
    void AddSubMenu(string label, Action? onClick, Action<ITrayMenuBuilder> buildChildren);

    /// <summary>
    /// Adds a separator between items.
    /// </summary>
    void AddSeparator();
}
