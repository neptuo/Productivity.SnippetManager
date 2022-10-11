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
    private const string Title = "Text from Clipboard";

    public Task InitializeAsync(SnippetProviderContext context)
        => Task.CompletedTask;

    public Task UpdateAsync(SnippetProviderContext context)
    {
        var existing = context.Models.FirstOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Models.Remove(existing);

        if (Clipboard.ContainsText())
        {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
                context.Models.Add(new SnippetModel(Title, text, priority: SnippetPriority.Most));
        }

        return Task.CompletedTask;
    }
}
