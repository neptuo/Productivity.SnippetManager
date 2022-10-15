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
    private readonly InlineSnippetConfiguration configuration;

    public InlineSnippetProvider(InlineSnippetConfiguration configuration) 
    => this.configuration = configuration;

    public Task InitializeAsync(SnippetProviderContext context)
    {
        if (configuration.Snippets != null)
            context.Snippets.AddRange(configuration.Snippets.Select(s => new SnippetModel(s.Key, s.Value)));
        
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SnippetProviderContext context) 
        => Task.CompletedTask;
}

public class InlineSnippetConfiguration : ProviderConfiguration
{
    public Dictionary<string, string>? Snippets { get; set; }

    public static InlineSnippetConfiguration Example => new InlineSnippetConfiguration() 
    {
        Snippets = new()
    };
}