using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// Cross-platform clipboard snippet provider. 
/// On macOS, clipboard history is not natively available, so only the current clipboard content is shown.
/// </summary>
public class ClipboardSnippetProvider : ISnippetProvider
{
    private const string Title = "Text from Clipboard";

    public Task InitializeAsync(SnippetProviderContext context)
        => Task.CompletedTask;

    public Task UpdateAsync(SnippetProviderContext context)
    {
        var existing = context.Models.SingleOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Remove(existing);

        // Use the Avalonia clipboard service which is set up by the Navigator
        // For now, we add the clipboard entry at the time we open the window
        // The actual clipboard text is fetched at the time of use
        context.Add(new SnippetModel(Title, priority: SnippetPriority.Most));

        return Task.CompletedTask;
    }
}
