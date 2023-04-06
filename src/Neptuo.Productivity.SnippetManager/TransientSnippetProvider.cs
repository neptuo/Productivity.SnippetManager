using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public abstract class TransientSnippetProvider : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context)
        => UpdateAsync(context);

    public abstract Task UpdateAsync(SnippetProviderContext context);
}
