using SharpHook.Native;
using Avalonia.Input;
using Neptuo.Productivity.SnippetManager.Views;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class HotkeyTests
{
    [Fact]
    public void MatchesModifiers_UsesSideAgnosticCompositeModifiers()
    {
        var currentModifiers = ModifierMask.LeftCtrl | ModifierMask.LeftShift;
        var expectedModifiers = ModifierMask.Ctrl | ModifierMask.Shift;

        bool result = Hotkey.MatchesModifiers(currentModifiers, expectedModifiers);

        Assert.True(result);
    }

    [Fact]
    public void MatchesModifiers_ReturnsFalseWhenARequiredModifierIsMissing()
    {
        var currentModifiers = ModifierMask.LeftCtrl | ModifierMask.LeftShift;
        var expectedModifiers = ModifierMask.Ctrl | ModifierMask.Shift | ModifierMask.Alt;

        bool result = Hotkey.MatchesModifiers(currentModifiers, expectedModifiers);

        Assert.False(result);
    }

    [Fact]
    public void IsCopyShortcutGesture_UsesPlatformSpecificModifier()
    {
        KeyModifiers expectedModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;
        KeyModifiers unexpectedModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Control
            : KeyModifiers.Meta;

        Assert.True(MainWindow.IsCopyShortcutGesture(Key.C, expectedModifier));
        Assert.False(MainWindow.IsCopyShortcutGesture(Key.C, unexpectedModifier));
    }
}
