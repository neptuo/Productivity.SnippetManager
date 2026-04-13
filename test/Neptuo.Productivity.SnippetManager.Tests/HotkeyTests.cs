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
    public void IsCopyShortcutGesture_MatchesPlatformModifier()
    {
        KeyModifiers expectedModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Meta
            : KeyModifiers.Control;

        Assert.True(MainWindow.IsCopyShortcutGesture(Key.C, expectedModifier));
    }

    [Fact]
    public void IsCopyShortcutGesture_RejectsWrongKey()
        => Assert.False(MainWindow.IsCopyShortcutGesture(Key.V, KeyModifiers.Control | KeyModifiers.Meta));

    [Fact]
    public void IsCopyShortcutGesture_RejectsOppositeModifier()
    {
        KeyModifiers wrongModifier = OperatingSystem.IsMacOS()
            ? KeyModifiers.Control
            : KeyModifiers.Meta;

        Assert.False(MainWindow.IsCopyShortcutGesture(Key.C, wrongModifier));
    }
}
