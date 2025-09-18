using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

public class InlineSnippetProvider(InlineSnippetConfiguration snippets) : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context)
    {
        context.AddRange(snippets.Select(s => new SnippetModel(s.Key, s.Value)));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SnippetProviderContext context) 
        => Task.CompletedTask;
}
