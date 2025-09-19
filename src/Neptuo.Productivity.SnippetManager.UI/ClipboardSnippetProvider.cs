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
        var existing = context.Models.SingleOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Remove(existing);

        bool hasRootSnippet = false;
        if (Clipboard.ContainsText())
        {
            string text = Clipboard.GetText();
            if (!string.IsNullOrEmpty(text) && !string.IsNullOrWhiteSpace(text))
            {
                context.Add(new SnippetModel(Title, text, priority: SnippetPriority.Most));
                hasRootSnippet = true;
            }
        }

        if (Clipboard2.IsHistoryEnabled())
        {
            var items = await Clipboard2.GetHistoryItemsAsync();
            foreach (var item in items.Items)
            {
                if (!hasRootSnippet)
                {
                    context.Add(new(Title, priority: SnippetPriority.Most));
                    hasRootSnippet = true;
                }

                string historyText = await item.Content.GetTextAsync();
                if (string.IsNullOrEmpty(historyText) || string.IsNullOrWhiteSpace(historyText))
                    continue;

                string title = historyText.Trim().Replace(SnippetModel.PathSeparator, " ");
                context.Add(new SnippetModel($"{Title} - {title}", historyText, priority: SnippetPriority.Low));
            }
        }
    }
}
