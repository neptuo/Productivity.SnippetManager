using System.ComponentModel.Composition;

namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Metadata describing an <see cref="ISnippetManagerPlugin"/> export.
/// </summary>
public interface ISnippetManagerPluginMetadata
{
    /// <summary>
    /// Human-readable name, used for diagnostics and ordering stability.
    /// Does not have to match any configuration key.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Load order hint. Lower values register first. Defaults to 0.
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Marks a class as an <see cref="ISnippetManagerPlugin"/> so it is
/// discovered by <see cref="PluginHost"/> via MEF.
/// </summary>
[MetadataAttribute]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SnippetManagerPluginAttribute : ExportAttribute, ISnippetManagerPluginMetadata
{
    public string Name { get; }

    public int Priority { get; set; }

    public SnippetManagerPluginAttribute(string name)
        : base(typeof(ISnippetManagerPlugin))
    {
        Name = name;
        Priority = 0;
    }
}
