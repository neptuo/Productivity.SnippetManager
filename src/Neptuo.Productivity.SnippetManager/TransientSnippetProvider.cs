namespace Neptuo.Productivity.SnippetManager;

public abstract class TransientSnippetProvider : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context)
        => UpdateAsync(context);

    public abstract Task UpdateAsync(SnippetProviderContext context);
}
