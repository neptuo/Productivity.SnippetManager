using System.ComponentModel.Composition;
using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager.Tests.Plugins;

public class PluginHostTests
{
    [Fact]
    public void Plugins_Are_Registered_In_Priority_Then_Name_Order()
    {
        RegistrationLog.Calls.Clear();

        using var host = new PluginHost();
        host.AddAssembly(typeof(PluginHostTests).Assembly);
        host.AddExportedValue<ISharedDependency>(new SharedDependency());

        var registry = new FakeRegistry();
        host.Compose(registry);

        Assert.Equal(
            new[] { "Alpha", "Bravo", "Charlie", "Delta" },
            RegistrationLog.Calls.ToArray());
    }

    [Fact]
    public void AddExportedValue_Is_Importable_By_Plugin()
    {
        RegistrationLog.Calls.Clear();
        BravoPlugin.LastImported = null;

        var shared = new SharedDependency();
        using var host = new PluginHost();
        host.AddAssembly(typeof(PluginHostTests).Assembly);
        host.AddExportedValue<ISharedDependency>(shared);

        var registry = new FakeRegistry();
        host.Compose(registry);

        Assert.Same(shared, BravoPlugin.LastImported);
    }

    [Fact]
    public void AddAssembly_Is_Idempotent_For_Same_Assembly()
    {
        RegistrationLog.Calls.Clear();

        using var host = new PluginHost();
        host.AddAssembly(typeof(PluginHostTests).Assembly);
        host.AddAssembly(typeof(PluginHostTests).Assembly);
        host.AddAssembly(typeof(PluginHostTests).Assembly);
        host.AddExportedValue<ISharedDependency>(new SharedDependency());

        var registry = new FakeRegistry();
        host.Compose(registry);

        Assert.Equal(4, RegistrationLog.Calls.Count);
        Assert.Equal(RegistrationLog.Calls.Distinct().Count(), RegistrationLog.Calls.Count);
    }

    public static class RegistrationLog
    {
        public static readonly List<string> Calls = new();
    }

    public interface ISharedDependency { }

    public sealed class SharedDependency : ISharedDependency { }

    private sealed class FakeRegistry : ISnippetProviderRegistry
    {
        public void Add<T>(string key, ISnippetProviderFactory<T> factory)
            where T : IProviderConfiguration<T> { }

        public void AddNotNullConfiguration<T>(string key, Func<T, ISnippetProvider?> providerFactory)
            where T : IProviderConfiguration<T> { }

        public void AddConfigChangeTracking<T>(string key, Func<T, ISnippetProvider> providerFactory, bool isNullConfigurationEnabled = false)
            where T : ProviderConfiguration, IEquatable<T>, IProviderConfiguration<T>, new() { }
    }
}

// Priority ties (Alpha and Bravo at 10) are broken by name.
// Then Charlie at 20, Delta at 30. Declaration order is intentionally
// scrambled so the test proves (Priority, Name) sorting is what matters.

[SnippetManagerPlugin("Delta", Priority = 30)]
public sealed class DeltaPlugin : ISnippetManagerPlugin
{
    public void Register(ISnippetProviderRegistry registry)
        => PluginHostTests.RegistrationLog.Calls.Add("Delta");
}

[SnippetManagerPlugin("Bravo", Priority = 10)]
public sealed class BravoPlugin : ISnippetManagerPlugin
{
    public static PluginHostTests.ISharedDependency? LastImported;

    [Import]
    public PluginHostTests.ISharedDependency Shared { get; set; } = default!;

    public void Register(ISnippetProviderRegistry registry)
    {
        LastImported = Shared;
        PluginHostTests.RegistrationLog.Calls.Add("Bravo");
    }
}

[SnippetManagerPlugin("Charlie", Priority = 20)]
public sealed class CharliePlugin : ISnippetManagerPlugin
{
    public void Register(ISnippetProviderRegistry registry)
        => PluginHostTests.RegistrationLog.Calls.Add("Charlie");
}

[SnippetManagerPlugin("Alpha", Priority = 10)]
public sealed class AlphaPlugin : ISnippetManagerPlugin
{
    public void Register(ISnippetProviderRegistry registry)
        => PluginHostTests.RegistrationLog.Calls.Add("Alpha");
}
