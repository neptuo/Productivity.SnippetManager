using Neptuo;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public abstract class SnippetProviderFactoryBase<T> : ISnippetProviderFactory<T>
    where T : ProviderConfiguration, IEquatable<T>, new()
{
    private ISnippetProvider? lastProvider;
    private T? lastConfiguration;
    private bool isNullConfigurationEnabled;

    public SnippetProviderFactoryBase(bool isNullConfigurationEnabled)
        => this.isNullConfigurationEnabled = isNullConfigurationEnabled;

    public bool TryCreate(T? configuration, out ISnippetProvider? provider)
    {
        if (isNullConfigurationEnabled && configuration == null)
            configuration = new T();

        if (configuration == null || !configuration.IsEnabled)
        {
            lastConfiguration = null;
            lastProvider = null;
            provider = null;
            return false;
        }

        if (lastConfiguration != null && IsEqual(lastConfiguration, configuration))
        {
            provider = lastProvider;
            return true;
        }

        lastConfiguration = configuration;
        provider = lastProvider = Create(configuration);
        return true;
    }

    protected abstract ISnippetProvider Create(T configuration);

    private bool IsEqual(T? last, T? current)
        => last != null && ((IEquatable<T>)last).Equals(current);
}

public class DelegateSnippetProviderFactory<T> : SnippetProviderFactoryBase<T>
    where T : ProviderConfiguration, IEquatable<T>, new()
{
    private readonly Func<T, ISnippetProvider> factory;

    public DelegateSnippetProviderFactory(Func<T, ISnippetProvider> factory, bool isNullConfigurationEnabled)
        : base(isNullConfigurationEnabled)
        => this.factory = factory;

    protected override ISnippetProvider Create(T configuration)
        => factory(configuration);
}

public class SimpleConfigurationSnippetProviderFactory<T> : ISnippetProviderFactory<T>
{
    private readonly Func<T, ISnippetProvider?> factory;

    public SimpleConfigurationSnippetProviderFactory(Func<T, ISnippetProvider?> factory)
        => this.factory = factory;

    public bool TryCreate(T? configuration, out ISnippetProvider? provider)
    {
        if (configuration != null)
        {
            provider = factory(configuration);
            return true;
        }

        provider = null;
        return false;
    }
}