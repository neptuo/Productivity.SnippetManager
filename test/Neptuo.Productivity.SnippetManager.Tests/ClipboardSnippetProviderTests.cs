using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class ClipboardSnippetProviderTests
{
    [Fact]
    public async Task UpdateAsync_AddsFilledSnippetFromClipboardText()
    {
        var context = new SnippetProviderContext();
        var provider = new ClipboardSnippetProvider(() => Task.FromResult<string?>("Copied text"));

        await provider.UpdateAsync(context);

        var snippet = Assert.Single(context.Models);
        Assert.Equal("Text from Clipboard", snippet.Title);
        Assert.Equal("Copied text", snippet.Text);
        Assert.True(snippet.IsFilled);
        Assert.Equal(SnippetPriority.Most, snippet.Priority);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateAsync_DoesNotAddSnippetWhenClipboardTextIsBlank(string? clipboardText)
    {
        var context = new SnippetProviderContext();
        var provider = new ClipboardSnippetProvider(() => Task.FromResult(clipboardText));

        await provider.UpdateAsync(context);

        Assert.Empty(context.Models);
    }
}
