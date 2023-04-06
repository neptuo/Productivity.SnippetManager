using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class SnippetProviderCollection
{
    private List<Func<Configuration, ISnippetProvider?>> storage = new List<Func<Configuration, ISnippetProvider?>>();

    public void Add<T>(Func<Configuration, T?> configurationSelector, ISnippetProviderFactory<T> factory)
    {
        storage.Add(configuration =>
        {
            var conf = configurationSelector(configuration);
            if (factory.TryCreate(conf, out var provider))
                return provider;

            return null;
        });
    }

    public void AddNotNullConfiguration<T>(Func<Configuration, T?> configurationSelector, Func<T, ISnippetProvider?> providerFactory)
    {
        Add(configurationSelector, new SimpleConfigurationSnippetProviderFactory<T>(providerFactory));
    }

    public void AddConfigChangeTracking<T>(Func<Configuration, T?> configurationSelector, Func<T, ISnippetProvider> providerFactory, bool isNullConfigurationEnabled = false)
        where T : ProviderConfiguration, IEquatable<T>, new()
    {
        Add(configurationSelector, new DelegateSnippetProviderFactory<T>(providerFactory, isNullConfigurationEnabled));
    }

    public ISnippetProvider Create(Configuration configuration)
    {
        var providers = new List<ISnippetProvider>();

        foreach (var factory in storage)
        {
            var provider = factory(configuration);
            if (provider != null)
                providers.Add(provider);
        }

        return new CompositeSnippetProvider(providers);
    }
}
