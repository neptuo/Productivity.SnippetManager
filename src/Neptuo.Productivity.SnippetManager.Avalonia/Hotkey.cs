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
        DiagnosticsLog.Info($"Binding global hotkey. Configured value: '{rawHotkey ?? "Control+Shift+V (default)"}'.");

        var key = KeyCode.VcV;
        var modifiers = ModifierMask.Ctrl | ModifierMask.Shift;
        if (!string.IsNullOrEmpty(rawHotkey) && !TryParseHotKey(rawHotkey, out key, out modifiers))
        {
            DiagnosticsLog.Error($"Unable to parse configured global hotkey '{rawHotkey}'.");
            navigator.Shutdown();
            return;
        }

        hotkey = (key, modifiers);
        openMainAction = () => navigator.OpenMain();
        DiagnosticsLog.Info($"Global hotkey parsed to key={key}, modifiers={modifiers}.");

        StartHook();
    }

    private void StartHook()
    {
        if (hook != null)
        {
            DiagnosticsLog.Info("Skipping global hotkey hook start because a hook instance already exists.");
            return;
        }

        hook = CreateHook();
        DiagnosticsLog.Info("Starting global hotkey hook.");
        StartHookAsync(hook);
    }

    private void OnKeyPressed(object? sender, KeyboardHookEventArgs e)
    {
        if (hotkey == null || openMainAction == null)
            return;

        var currentModifiers = e.RawEvent.Mask;
        bool modifiersMatch = MatchesModifiers(currentModifiers, hotkey.Value.modifiers);
        bool keyMatch = e.Data.KeyCode == hotkey.Value.key;

        LogRelevantKeyEvent("pressed", e, keyMatch, modifiersMatch);

        if (modifiersMatch && keyMatch)
        {
            DiagnosticsLog.Info("Global hotkey matched. Suppressing event and dispatching the open-main action.");
            e.SuppressEvent = true;
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DiagnosticsLog.Info("Executing the global hotkey action on the UI thread.");
                openMainAction();
            });
        }
    }

    private void OnKeyReleased(object? sender, KeyboardHookEventArgs e)
        => LogRelevantKeyEvent("released", e, keyMatch: false, modifiersMatch: false);

    private void OnHookEnabled(object? sender, HookEventArgs e)
        => DiagnosticsLog.Info("Global hotkey hook enabled.");

    private void OnHookDisabled(object? sender, HookEventArgs e)
        => DiagnosticsLog.Info("Global hotkey hook disabled.");

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
            DiagnosticsLog.Info("Disposing the global hotkey hook.");
            hook.HookEnabled -= OnHookEnabled;
            hook.HookDisabled -= OnHookDisabled;
            hook.KeyPressed -= OnKeyPressed;
            hook.KeyReleased -= OnKeyReleased;
            hook.Dispose();
            hook = null;
        }
    }

    public void Pause()
    {
        DiagnosticsLog.Info("Pausing the global hotkey hook.");
        UnBind();
    }

    public void Restore()
    {
        if (hotkey != null)
        {
            DiagnosticsLog.Info("Restoring the global hotkey hook.");
            hook ??= CreateHook();

            if (!hook.IsRunning)
            {
                DiagnosticsLog.Info("Restarting the global hotkey hook.");
                StartHookAsync(hook);
            }
            else
            {
                DiagnosticsLog.Info("The global hotkey hook is already running.");
            }
        }
    }

    public void Dispose()
        => UnBind();

    private SimpleGlobalHook CreateHook()
    {
        var createdHook = new SimpleGlobalHook(GlobalHookType.Keyboard);
        createdHook.HookEnabled += OnHookEnabled;
        createdHook.HookDisabled += OnHookDisabled;
        createdHook.KeyPressed += OnKeyPressed;
        createdHook.KeyReleased += OnKeyReleased;
        return createdHook;
    }

    private static void StartHookAsync(SimpleGlobalHook hook)
    {
        Task runTask = hook.RunAsync();
        _ = runTask.ContinueWith(
            task => DiagnosticsLog.Error("The global hotkey hook terminated with an exception.", task.Exception),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    private void LogRelevantKeyEvent(string eventName, KeyboardHookEventArgs e, bool keyMatch, bool modifiersMatch)
    {
        if (hotkey == null)
            return;

        var currentModifiers = e.RawEvent.Mask;
        if (!ShouldLogKeyEvent(e.Data.KeyCode, currentModifiers))
            return;

        DiagnosticsLog.Info(
            $"Global hotkey key {eventName}: key={e.Data.KeyCode}, rawCode={e.Data.RawCode}, mask={currentModifiers}, " +
            $"ctrl={currentModifiers.HasCtrl()}, shift={currentModifiers.HasShift()}, alt={currentModifiers.HasAlt()}, " +
            $"meta={currentModifiers.HasMeta()}, keyMatch={keyMatch}, modifiersMatch={modifiersMatch}.");
    }

    private bool ShouldLogKeyEvent(KeyCode keyCode, ModifierMask currentModifiers)
        => hotkey != null && (keyCode == hotkey.Value.key || currentModifiers != ModifierMask.None);

    internal static bool MatchesModifiers(ModifierMask currentModifiers, ModifierMask expectedModifiers)
    {
        if (expectedModifiers.HasCtrl() && !currentModifiers.HasCtrl())
            return false;

        if (expectedModifiers.HasShift() && !currentModifiers.HasShift())
            return false;

        if (expectedModifiers.HasAlt() && !currentModifiers.HasAlt())
            return false;

        if (expectedModifiers.HasMeta() && !currentModifiers.HasMeta())
            return false;

        return true;
    }
}
