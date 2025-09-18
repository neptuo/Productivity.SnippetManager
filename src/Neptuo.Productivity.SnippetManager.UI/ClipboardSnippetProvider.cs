using System.Windows.Forms;
using Neptuo.Productivity.SnippetManager.Models;
using Clipboard2 = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Neptuo.Productivity.SnippetManager;

public class ClipboardSnippetProvider : ISnippetProvider
{
    private const string Title = "Text from Clipboard";

    public Task InitializeAsync(SnippetProviderContext context)
        => Task.CompletedTask;

    public async Task UpdateAsync(SnippetProviderContext context)
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

        if (Clipboard2.IsHistoryEnabled())
        {
            var items = await Clipboard2.GetHistoryItemsAsync();
            foreach (var item in items.Items)
            {
                string text = await item.Content.GetTextAsync();
                context.Add(new SnippetModel($"{Title} - {text.Trim()}", text));
            }
        }
    }
}
