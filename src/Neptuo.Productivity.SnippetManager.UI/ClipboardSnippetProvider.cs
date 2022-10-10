using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public class ClipboardSnippetProvider : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context) 
        => Task.CompletedTask;

    public Task UpdateAsync(SnippetProviderContext context)
    {
        if (Clipboard.ContainsText())
            SnippetModel.SingleCollection("Text from Clipboard", Clipboard.GetText(), priority: SnippetPriority.Most);

        return Task.CompletedTask;
    }
}
