using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class CompositeSnippetProvider : ISnippetProvider
{
    private readonly IEnumerable<ISnippetProvider> snippetProviders;

    public CompositeSnippetProvider(params ISnippetProvider[] snippetProviders) 
        => this.snippetProviders = snippetProviders;

    public async Task<IReadOnlyCollection<SnippetModel>> GetAsync()
    {
        var snippets = await Task.WhenAll(snippetProviders.Select(p => p.GetAsync()));
        var result = new List<SnippetModel>();
        snippets.ForEach(result.AddRange);
        return result;
    }
}
