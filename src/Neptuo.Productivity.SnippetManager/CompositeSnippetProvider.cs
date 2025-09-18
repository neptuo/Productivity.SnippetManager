namespace Neptuo.Productivity.SnippetManager;

public class CompositeSnippetProvider : ISnippetProvider
{
    private readonly IEnumerable<ISnippetProvider> snippetProviders;

    public CompositeSnippetProvider(IEnumerable<ISnippetProvider> snippetProviders) 
        => this.snippetProviders = snippetProviders;

    public Task InitializeAsync(SnippetProviderContext context) 
        => Task.WhenAll(snippetProviders.Select(p => p.InitializeAsync(context)));

    public Task UpdateAsync(SnippetProviderContext context)
        => Task.WhenAll(snippetProviders.Select(p => p.UpdateAsync(context)));
}
