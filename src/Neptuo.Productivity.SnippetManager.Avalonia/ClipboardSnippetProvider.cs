using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

/// <summary>
/// Cross-platform clipboard snippet provider. 
/// On macOS, clipboard history is not natively available, so only the current clipboard content is shown.
/// </summary>
public class ClipboardSnippetProvider : ISnippetProvider
{
    private const string Title = "Text from Clipboard";
    private readonly Func<Task<string?>> getTextAsync;

    public ClipboardSnippetProvider(Func<Task<string?>>? getTextAsync = null)
    {
        this.getTextAsync = getTextAsync ?? GetClipboardTextAsync;
    }

    public Task InitializeAsync(SnippetProviderContext context)
        => Task.CompletedTask;

    public async Task UpdateAsync(SnippetProviderContext context)
    {
        var existing = context.Models.SingleOrDefault(m => m.Title == Title);
        if (existing != null)
            context.Remove(existing);

        string? text = await getTextAsync();
        if (string.IsNullOrWhiteSpace(text))
            return;

        context.Add(new SnippetModel(Title, text, priority: SnippetPriority.Most));
    }

    private static async Task<string?> GetClipboardTextAsync()
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
            return null;

#pragma warning disable CS0618 // GetTextAsync is deprecated in favor of TryGetTextAsync in Avalonia 11.3+
        return await clipboard.GetTextAsync();
#pragma warning restore CS0618
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;

        if (desktop.MainWindow is { } mainWindow)
            return TopLevel.GetTopLevel(mainWindow)?.Clipboard;

        if (desktop.Windows.LastOrDefault() is { } window)
            return TopLevel.GetTopLevel(window)?.Clipboard;

        return null;
    }
}
