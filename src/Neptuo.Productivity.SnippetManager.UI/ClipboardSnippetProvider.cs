using System.Windows.Forms;
using Neptuo.Productivity.SnippetManager.Models;
using Clipboard2 = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Neptuo.Productivity.SnippetManager;

public class ClipboardSnippetProvider : TransientSnippetProvider
{
    private const string Title = "Text from Clipboard";

    public override async Task UpdateAsync(SnippetProviderContext context)
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

        var items = await Clipboard2.GetHistoryItemsAsync();
        foreach (var item in items.Items.OrderByDescending(i => i.Timestamp))
        {
            string text = await item.Content.GetTextAsync();
            context.Add(new SnippetModel($"{Title} - {text.Trim()}", text));
        }
    }
}
