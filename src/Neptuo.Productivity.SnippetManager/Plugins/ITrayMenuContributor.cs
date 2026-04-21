namespace Neptuo.Productivity.SnippetManager.Plugins;

/// <summary>
/// Plugin extension point for contributing items to the application's tray
/// (status bar) menu. Implementations are discovered via MEF using
/// <c>[Export(typeof(ITrayMenuContributor))]</c> — typically applied to the
/// same class that also implements <see cref="ISnippetManagerPlugin"/>.
/// <para>
/// Hosts invoke <see cref="Contribute"/> every time the tray menu opens, so
/// contributions can reflect state that changes at runtime (e.g. a file list
/// that grows as the user edits configuration).
/// </para>
/// </summary>
public interface ITrayMenuContributor
{
    void Contribute(ITrayMenuBuilder menu);
}
