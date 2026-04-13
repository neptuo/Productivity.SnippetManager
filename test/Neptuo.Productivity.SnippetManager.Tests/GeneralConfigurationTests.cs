namespace Neptuo.Productivity.SnippetManager.Tests;

public class GeneralConfigurationTests
{
    [Fact]
    public void DefaultHotKey_IsOneOfTheSupportedPlatformDefaults()
    {
        Assert.Contains(
            GeneralConfiguration.DefaultHotKey,
            new[] { GeneralConfiguration.MacDefaultHotKey, GeneralConfiguration.NonMacDefaultHotKey }
        );
    }

    [Fact]
    public void Example_UsesTheDefaultHotKey()
        => Assert.Equal(GeneralConfiguration.DefaultHotKey, GeneralConfiguration.Example.HotKey);
}
