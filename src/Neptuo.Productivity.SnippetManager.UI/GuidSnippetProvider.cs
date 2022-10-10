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
    public Task InitializeAsync(SnippetProviderContext context) 
        => Task.CompletedTask;

    public Task UpdateAsync(SnippetProviderContext context)
    {
        context.Models.Add(SnippetModel.SingleCollection("GUID", Guid.NewGuid().ToString()));
        return Task.CompletedTask;
    }
}
