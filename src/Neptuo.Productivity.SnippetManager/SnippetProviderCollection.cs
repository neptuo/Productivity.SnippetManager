using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class SnippetProviderCollection
{
    private Dictionary<string, (Type configurationType, Func<Dictionary<string, object>, ISnippetProvider?> factory, Func<object> exampleConfiguration)> storage = new();

    public void Add<T>(string key, ISnippetProviderFactory<T> factory)
        where T : IProviderConfiguration<T>
    {
        storage[key] = (
            typeof(T), 
            configuration =>
            {
                configuration.TryGetValue(key, out var conf);
                if (factory.TryCreate((T?)conf, out var provider))
                    return provider;

                return null;
            }, 
            () => T.Example
        );
    }

    public void AddNotNullConfiguration<T>(string key, Func<T, ISnippetProvider?> providerFactory)
        where T : IProviderConfiguration<T>
    {
        Add(key, new SimpleConfigurationSnippetProviderFactory<T>(providerFactory));
    }

    public void AddConfigChangeTracking<T>(string key, Func<T, ISnippetProvider> providerFactory, bool isNullConfigurationEnabled = false)
        where T : ProviderConfiguration, IEquatable<T>, IProviderConfiguration<T>, new()
    {
        Add(key, new DelegateSnippetProviderFactory<T>(providerFactory, isNullConfigurationEnabled));
    }

    public ISnippetProvider Create(Dictionary<string, object> configuration)
    {
        var providers = new List<ISnippetProvider>();

        foreach (var entry in storage)
        {
            var provider = entry.Value.factory(configuration);
            if (provider != null)
                providers.Add(provider);
        }

        return new CompositeSnippetProvider(providers);
    }

    public IEnumerable<(string key, Type configurationType)> GetConfigurationMappings() 
        => storage.Select(s => (s.Key, s.Value.configurationType));

    public void AddExampleConfigurations(Dictionary<string, object> examples)
    {
        foreach (var entry in storage)
            examples[entry.Key] = entry.Value.exampleConfiguration();
    }
}
