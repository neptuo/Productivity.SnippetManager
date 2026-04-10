using SharpHook;
using SharpHook.Native;
using Avalonia.Threading;

namespace Neptuo.Productivity.SnippetManager;

public class Hotkey : IDisposable
{
    private SimpleGlobalHook? hook;
    private (KeyCode key, ModifierMask modifiers)? hotkey;
    private Action? openMainAction;

    public void Bind(Navigator navigator, string? rawHotkey)
    {
        var key = KeyCode.VcV;
        var modifiers = ModifierMask.Ctrl | ModifierMask.Shift;
        if (!string.IsNullOrEmpty(rawHotkey) && !TryParseHotKey(rawHotkey, out key, out modifiers))
        {
            navigator.Shutdown();
            return;
        }

        hotkey = (key, modifiers);
        openMainAction = () => navigator.OpenMain();

        StartHook();
    }

    private void StartHook()
    {
        if (hook != null)
            return;

        hook = new SimpleGlobalHook();
        hook.KeyPressed += OnKeyPressed;
        Task.Run(() => hook.Run());
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (hotkey == null || openMainAction == null)
            return;

        var currentModifiers = e.RawEvent.Mask;
        bool modifiersMatch = (currentModifiers & hotkey.Value.modifiers) == hotkey.Value.modifiers;

        if (modifiersMatch && e.Data.KeyCode == hotkey.Value.key)
        {
            e.SuppressEvent = true;
            Dispatcher.UIThread.InvokeAsync(openMainAction);
        }
    }

    private bool TryParseHotKey(string rawHotkey, out KeyCode key, out ModifierMask modifiers)
    {
        modifiers = ModifierMask.None;
        key = KeyCode.VcUndefined;

        string[] parts = rawHotkey.Split('+', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (i < parts.Length - 1)
            {
                // Modifier
                switch (part.ToLowerInvariant())
                {
                    case "control":
                    case "ctrl":
                        modifiers |= ModifierMask.Ctrl;
                        break;
                    case "shift":
                        modifiers |= ModifierMask.Shift;
                        break;
                    case "alt":
                        modifiers |= ModifierMask.Alt;
                        break;
                    case "meta":
                    case "windows":
                    case "command":
                    case "cmd":
                        modifiers |= ModifierMask.Meta;
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                // Key
                if (!TryParseKey(part, out key))
                    return false;
            }
        }

        return true;
    }

    private static bool TryParseKey(string keyName, out KeyCode key)
    {
        key = keyName.ToUpperInvariant() switch
        {
            "A" => KeyCode.VcA,
            "B" => KeyCode.VcB,
            "C" => KeyCode.VcC,
            "D" => KeyCode.VcD,
            "E" => KeyCode.VcE,
            "F" => KeyCode.VcF,
            "G" => KeyCode.VcG,
            "H" => KeyCode.VcH,
            "I" => KeyCode.VcI,
            "J" => KeyCode.VcJ,
            "K" => KeyCode.VcK,
            "L" => KeyCode.VcL,
            "M" => KeyCode.VcM,
            "N" => KeyCode.VcN,
            "O" => KeyCode.VcO,
            "P" => KeyCode.VcP,
            "Q" => KeyCode.VcQ,
            "R" => KeyCode.VcR,
            "S" => KeyCode.VcS,
            "T" => KeyCode.VcT,
            "U" => KeyCode.VcU,
            "V" => KeyCode.VcV,
            "W" => KeyCode.VcW,
            "X" => KeyCode.VcX,
            "Y" => KeyCode.VcY,
            "Z" => KeyCode.VcZ,
            "SPACE" => KeyCode.VcSpace,
            "F1" => KeyCode.VcF1,
            "F2" => KeyCode.VcF2,
            "F3" => KeyCode.VcF3,
            "F4" => KeyCode.VcF4,
            "F5" => KeyCode.VcF5,
            "F6" => KeyCode.VcF6,
            "F7" => KeyCode.VcF7,
            "F8" => KeyCode.VcF8,
            "F9" => KeyCode.VcF9,
            "F10" => KeyCode.VcF10,
            "F11" => KeyCode.VcF11,
            "F12" => KeyCode.VcF12,
            _ => KeyCode.VcUndefined
        };

        return key != KeyCode.VcUndefined;
    }

    public void UnBind()
    {
        if (hook != null)
        {
            hook.KeyPressed -= OnKeyPressed;
            hook.Dispose();
            hook = null;
        }
    }

    public void Pause()
        => UnBind();

    public void Restore()
    {
        if (hotkey != null)
        {
            hook ??= new SimpleGlobalHook();
            hook.KeyPressed += OnKeyPressed;

            if (!hook.IsRunning)
                Task.Run(() => hook.Run());
        }
    }

    public void Dispose()
        => UnBind();
}
