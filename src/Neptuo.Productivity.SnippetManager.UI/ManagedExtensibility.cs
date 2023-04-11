using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class ManagedExtensibility
{
    private CompositionContainer? container;

    public void Initialize()
    {
        try
        {
            //new DirectoryCatalog(".");

            var catalog = new AggregateCatalog();
            catalog.Catalogs.Add(new AssemblyCatalog(typeof(ManagedExtensibility).Assembly));

            container = new CompositionContainer(catalog);


            var helper = new SnippetProviderHelper();
            container.ComposeParts(helper);
        }
        catch (CompositionException compositionException)
        {
            Console.WriteLine(compositionException.ToString());
        }
    }
}

class SnippetProviderHelper
{
    [ImportMany]
    public IEnumerable<Lazy<ISnippetProvider, IConfigChangeTrackingSnippetProviderMetadata>> Providers { get; set; }
}

public interface IConfigChangeTrackingSnippetProviderMetadata
{
    string ConfigurationKey { get; }
    Type ConfigurationType { get; }
    bool IsNullConfigurationEnabled { get; }
}

[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ConfigChangeTrackingSnippetProviderAttribute<T> : ExportAttribute, IConfigChangeTrackingSnippetProviderMetadata
{
    public string ConfigurationKey { get; set; }
    public Type ConfigurationType { get; set; }
    public bool IsNullConfigurationEnabled { get; set; }

    public ConfigChangeTrackingSnippetProviderAttribute(string configurationKey, bool isNullConfigurationEnabled = false)
        : base(typeof(ISnippetProvider))
    {
        ConfigurationKey = configurationKey;
        ConfigurationType = typeof(T);
        IsNullConfigurationEnabled = isNullConfigurationEnabled;
    }
}

[ConfigChangeTrackingSnippetProvider<GitHubConfiguration>("GitHub")]
public class TestSnippetProvider : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(SnippetProviderContext context)
    {
        throw new NotImplementedException();
    }
}
