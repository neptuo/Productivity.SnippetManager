namespace Neptuo.Productivity.SnippetManager;

public class CompositeSnippetProvider(IEnumerable<ISnippetProvider> snippetProviders) : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context) 
        => Task.WhenAll(snippetProviders.Select(p => p.InitializeAsync(context)));

    public Task UpdateAsync(SnippetProviderContext context)
        => Task.WhenAll(snippetProviders.Select(p => p.UpdateAsync(context)));
}
