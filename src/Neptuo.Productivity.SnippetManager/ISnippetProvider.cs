namespace Neptuo.Productivity.SnippetManager;

public interface ISnippetProvider
{
    Task InitializeAsync(SnippetProviderContext context);
    Task UpdateAsync(SnippetProviderContext context);
}
