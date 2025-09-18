using Neptuo.Windows.HotKeys;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Neptuo.Productivity.SnippetManager;

public class Hotkey
{
    private readonly ComponentDispatcherHotkeyCollection hotkeys = new ComponentDispatcherHotkeyCollection();
    private (Key key, ModifierKeys modifiers)? hotkey;
    private Action? bindLastHotkey;

    public void Bind(Navigator navigator, Dispatcher dispatcher, string? rawHotkey)
    {
        var key = Key.V;
        var modifiers = ModifierKeys.Control | ModifierKeys.Shift;
        if (!String.IsNullOrEmpty(rawHotkey) && !TryParseHotKey(rawHotkey, out key, out modifiers))
        {
            MessageBox.Show("Error in hotkey configuration", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
            navigator.Shutdown();
        }

        hotkey = (key, modifiers);

        bindLastHotkey = () =>
        {
            try
            {
                hotkeys.Add(key, modifiers, (_, _) => navigator.OpenMain());
            }
            catch (Win32Exception)
            {
                MessageBox.Show("The configured hotkey is probably taken by other application. Edit your configuration.", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                navigator.OpenConfiguration();
                dispatcher.Invoke(() => navigator.Shutdown());
            }
        };
        bindLastHotkey();
    }

    private bool TryParseHotKey(string hotKey, out Key key, out ModifierKeys modifiers)
    {
        modifiers = ModifierKeys.None;
        key = Key.None;

        string[] parts = hotKey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (i < parts.Length - 1)
                {
                    if (Enum.TryParse<ModifierKeys>(parts[i], out var mod))
                    {
                        if (i == 0)
                            modifiers = mod;
                        else
                            modifiers |= mod;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    if (Enum.TryParse<Key>(parts[i], out var k))
                        key = k;
                    else
                        return false;
                }
            }
        }

        return true;
    }

    public void UnBind()
    {
        if (hotkey != null)
            hotkeys.Remove(hotkey.Value.key, hotkey.Value.modifiers);
    }

    public void Pause() 
        => UnBind();

    public void Restore()
    {
        if (bindLastHotkey != null)
            bindLastHotkey();
    }
}
