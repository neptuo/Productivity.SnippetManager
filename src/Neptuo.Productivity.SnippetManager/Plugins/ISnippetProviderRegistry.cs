namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Registration surface exposed to <see cref="ISnippetManagerPlugin"/>
/// implementations. Mirrors the subset of <see cref="SnippetProviderCollection"/>
/// that plugins need so the host can swap the underlying collection without
/// touching plugin code.
/// </summary>
public interface ISnippetProviderRegistry
{
    void Add<T>(string key, ISnippetProviderFactory<T> factory)
        where T : IProviderConfiguration<T>;

    void AddNotNullConfiguration<T>(string key, Func<T, ISnippetProvider?> providerFactory)
        where T : IProviderConfiguration<T>;

    void AddConfigChangeTracking<T>(string key, Func<T, ISnippetProvider> providerFactory, bool isNullConfigurationEnabled = false)
        where T : ProviderConfiguration, IEquatable<T>, IProviderConfiguration<T>, new();
}
