using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Reflection;

namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Discovers <see cref="ISnippetManagerPlugin"/> exports via MEF from a
/// caller-supplied set of assemblies and registers them into an
/// <see cref="ISnippetProviderRegistry"/>. The underlying
/// <see cref="CompositionContainer"/> stays alive after <see cref="Compose"/>
/// so hosts can resolve additional plugin-contributed exports (for example
/// tray-menu contributors) from the same catalog.
/// </summary>
public sealed class PluginHost : IDisposable
{
    private readonly AggregateCatalog catalog = new();
    private readonly HashSet<Assembly> assemblies = new();
    private readonly List<Action<CompositionContainer>> pendingExports = new();
    private CompositionContainer? container;
    private bool composed;

    /// <summary>
    /// Exports <paramref name="value"/> as <typeparamref name="TContract"/> into
    /// the composition container so plugin parts can <c>[Import]</c> it. Must be
    /// called before <see cref="Compose"/>.
    /// </summary>
    public void AddExportedValue<TContract>(TContract value) where TContract : class
    {
        if (composed)
            throw Ensure.Exception.InvalidOperation($"{nameof(AddExportedValue)} must be called before {nameof(Compose)}.");

        pendingExports.Add(c => c.ComposeExportedValue<TContract>(value));
    }

    /// <summary>
    /// Adds an assembly to the MEF catalog. No-op if the assembly has already
    /// been added. Must be called before <see cref="Compose"/>.
    /// </summary>
    public void AddAssembly(Assembly assembly)
    {
        if (composed)
            throw Ensure.Exception.InvalidOperation($"{nameof(AddAssembly)} must be called before {nameof(Compose)}.");

        if (assemblies.Add(assembly))
            catalog.Catalogs.Add(new AssemblyCatalog(assembly));
    }

    /// <summary>
    /// Composes all discovered plugins and registers them into
    /// <paramref name="registry"/> in ascending
    /// <see cref="ISnippetManagerPluginMetadata.Priority"/> order (ties
    /// broken by plugin name for stability). Returns the composition
    /// container so callers can resolve other plugin-contributed services.
    /// </summary>
    public CompositionContainer Compose(ISnippetProviderRegistry registry)
    {
        if (composed)
            throw Ensure.Exception.InvalidOperation($"{nameof(Compose)} has already been called on this {nameof(PluginHost)}.");

        composed = true;
        container = new CompositionContainer(catalog);

        foreach (var export in pendingExports)
            export(container);

        var importer = new Importer();
        try
        {
            container.ComposeParts(importer);
        }
        catch (CompositionException ex)
        {
            Debug.WriteLine($"Plugin composition failed: {ex}");
            throw;
        }

        foreach (var plugin in importer.Plugins.OrderBy(p => p.Metadata.Priority).ThenBy(p => p.Metadata.Name))
            plugin.Value.Register(registry);

        return container;
    }

    public void Dispose()
    {
        container?.Dispose();
        catalog.Dispose();
    }

    private sealed class Importer
    {
        [ImportMany]
        public IEnumerable<Lazy<ISnippetManagerPlugin, ISnippetManagerPluginMetadata>> Plugins { get; set; }
            = Array.Empty<Lazy<ISnippetManagerPlugin, ISnippetManagerPluginMetadata>>();
    }
}
