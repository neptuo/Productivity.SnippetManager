using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public class ClipboardSnippetProvider : TransientSnippetProvider
{
    private const string Title = "Text from Clipboard";

    public override Task UpdateAsync(SnippetProviderContext context)
    {
        var existing = context.Models.FirstOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Remove(existing);

        if (Clipboard.ContainsText())
        {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                context.Add(new SnippetModel(Title, text, priority: SnippetPriority.Most));
        }

        return Task.CompletedTask;
    }
}
