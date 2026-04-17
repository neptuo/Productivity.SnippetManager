using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Variables;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;

namespace Neptuo.Productivity.SnippetManager.Views.DesignData;

internal class ViewModelLocator
{
    private static readonly SnippetExpansionPipeline emptyPipeline = new SnippetExpansionPipeline(
        new TokenSnippetVariableScanner(),
        new ConfigurationVariableValueResolver(null),
        new TokenSnippetTextExpander()
    );

    private static MainViewModel? mainViewModel;
    public static MainViewModel MainViewModel
    {
        get
        {
            if (mainViewModel == null)
            {
                var tree = LoadSnippets();

                mainViewModel = new MainViewModel(
                    tree,
                    new ApplySnippetCommand(InteropService.Instance, emptyPipeline),
                    new CopySnippetCommand(InteropService.Instance, emptyPipeline)
                );

                mainViewModel.Snippets.AddRange(tree.GetRoots());
                mainViewModel.Search("");
                mainViewModel.IsInitializing = false;
            }

            return mainViewModel;
        }
    }

    private static ISnippetTree LoadSnippets()
    {
        SnippetProviderContext ctx = new SnippetProviderContext();

        void Add(string title, string text, string? description = null)
            => ctx.Add(new SnippetModel(title, text, description, SnippetPriority.High));

        Add("C# class", "using System;", "public class Class...");
        Add("Maps", "https://maps.google.com");
        Add("GitHub - dotnet - runtime", "https://github.com/dotnet/runtime");
        Add("Money", "https://app.money.neptuo.com");
        return ctx;
    }
}
