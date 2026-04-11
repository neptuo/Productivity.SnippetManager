using SharpHook.Native;

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
}
