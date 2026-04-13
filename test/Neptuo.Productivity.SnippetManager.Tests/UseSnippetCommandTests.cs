using System.Windows.Input;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class UseSnippetCommandTests
{
    [Fact]
    public void CopyCommand_CanExecute_ReturnsFalseForNullParameter()
    {
        ICommand command = new CopySnippetCommand(new TestClipboardService());

        bool canExecute = command.CanExecute(null);

        Assert.False(canExecute);
    }

    [Fact]
    public void CopyCommand_CanExecute_ReturnsFalseForShadowSnippet()
    {
        var command = new CopySnippetCommand(new TestClipboardService());

        bool canExecute = command.CanExecute(new SnippetModel("GitHub"));

        Assert.False(canExecute);
    }

    [Fact]
    public void CopyCommand_CanExecute_ReturnsTrueForFilledSnippet()
    {
        var command = new CopySnippetCommand(new TestClipboardService());

        bool canExecute = command.CanExecute(new SnippetModel("GitHub - dotnet", "https://github.com/dotnet"));

        Assert.True(canExecute);
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public void SetText(string text)
        { }
    }
}
