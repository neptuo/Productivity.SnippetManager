namespace Neptuo.Productivity.SnippetManager.Tests;

public class GeneralConfigurationTests
{
    [Fact]
    public void DefaultHotKey_UsesThePlatformSpecificShortcut()
    {
        string expected = OperatingSystem.IsMacOS()
            ? GeneralConfiguration.MacDefaultHotKey
            : GeneralConfiguration.WindowsDefaultHotKey;

        Assert.Equal(expected, GeneralConfiguration.DefaultHotKey);
    }

    [Fact]
    public void Example_UsesTheDefaultHotKey()
        => Assert.Equal(GeneralConfiguration.DefaultHotKey, GeneralConfiguration.Example.HotKey);
}
