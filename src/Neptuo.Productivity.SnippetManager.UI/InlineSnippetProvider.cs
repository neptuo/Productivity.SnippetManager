using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public class InlineSnippetProvider : ISnippetProvider
{
    private readonly Dictionary<string, string> snippets;

    public InlineSnippetProvider(Dictionary<string, string> snippets) 
        => this.snippets = snippets;

    public Task InitializeAsync(SnippetProviderContext context)
    {
        context.AddRange(snippets.Select(s => new SnippetModel(s.Key, s.Value)));
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SnippetProviderContext context) 
        => Task.CompletedTask;
}
