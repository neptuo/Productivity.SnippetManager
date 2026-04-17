using System.Windows.Input;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.Variables;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class CopySnippetCommandTests
{
    private static SnippetExpansionPipeline EmptyPipeline => new SnippetExpansionPipeline(
        new TokenSnippetTemplateCompiler(),
        new ConfigurationVariableValueResolver(null)
    );

    [Fact]
    public void CopyCommand_CanExecute_ReturnsFalseForNullParameter()
    {
        ICommand command = new CopySnippetCommand(new TestClipboardService(), EmptyPipeline);

        bool canExecute = command.CanExecute(null);

        Assert.False(canExecute);
    }

    [Fact]
    public void CopyCommand_CanExecute_ReturnsFalseForShadowSnippet()
    {
        var command = new CopySnippetCommand(new TestClipboardService(), EmptyPipeline);

        bool canExecute = command.CanExecute(new SnippetModel("GitHub"));

        Assert.False(canExecute);
    }

    [Fact]
    public void CopyCommand_CanExecute_ReturnsTrueForFilledSnippet()
    {
        var command = new CopySnippetCommand(new TestClipboardService(), EmptyPipeline);

        bool canExecute = command.CanExecute(new SnippetModel("GitHub - dotnet", "https://github.com/dotnet"));

        Assert.True(canExecute);
    }

    [Fact]
    public void CopyCommand_Execute_ExpandsVariablesBeforeSettingClipboard()
    {
        var service = new TestClipboardService();
        var config = new VariablesConfiguration { ["ShellExt"] = "ps1" };
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(config)
        );
        var command = new CopySnippetCommand(service, pipeline);

        command.Execute(new SnippetModel("Install", "install.{ShellExt}"));

        Assert.Equal("install.ps1", service.LastText);
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public string? LastText { get; private set; }

        public void SetText(string text)
            => LastText = text;
    }
}
