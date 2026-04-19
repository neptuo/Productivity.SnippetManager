namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// A self-contained registration unit that owns everything a single snippet
/// provider (or a related group) needs: its configuration key(s),
/// configuration type(s), factory and example configuration. Host
/// applications enumerate plugins discovered via MEF instead of hard-coding
/// provider wiring.
/// </summary>
public interface ISnippetManagerPlugin
{
    /// <summary>
    /// Contribute this plugin's snippet providers into the given
    /// <paramref name="registry"/>. Called once at application startup.
    /// </summary>
    void Register(ISnippetProviderRegistry registry);
}
