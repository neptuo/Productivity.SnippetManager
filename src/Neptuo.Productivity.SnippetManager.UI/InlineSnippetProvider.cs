using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

public class InlineSnippetProvider : ISnippetProvider
{
    private readonly Dictionary<string, string> snippets;

    public InlineSnippetProvider(InlineSnippetConfiguration snippets) 
        => this.snippets = snippets;

    public Task InitializeAsync(SnippetProviderContext context)
    {
        context.AddRange(snippets.Select(s => new SnippetModel(s.Key, s.Value)));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SnippetProviderContext context) 
        => Task.CompletedTask;
}
