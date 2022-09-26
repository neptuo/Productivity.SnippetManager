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
    public Task<IReadOnlyCollection<SnippetModel>> GetAsync()
    {
        string guid = Guid.NewGuid().ToString();
        return Task.FromResult<IReadOnlyCollection<SnippetModel>>(
            new[] { 
                new SnippetModel()
                {
                    Title = "GUID",
                    Text =guid,
                    Description = guid
                }
            }
        );
    }
}
