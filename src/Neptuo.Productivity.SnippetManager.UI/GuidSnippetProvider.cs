using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class GuidSnippetProvider : ISnippetProvider
{
    private const string Title = "GUID";

    public Task InitializeAsync(SnippetProviderContext context) 
        => Task.CompletedTask;

    public Task UpdateAsync(SnippetProviderContext context)
    {
        var existing = context.Models.FirstOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Models.Remove(existing);

        context.Models.Add(new SnippetModel(Title, Guid.NewGuid().ToString()));
        return Task.CompletedTask;
    }
}
