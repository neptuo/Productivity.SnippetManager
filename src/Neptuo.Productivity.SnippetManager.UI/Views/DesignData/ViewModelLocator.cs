using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;

namespace Neptuo.Productivity.SnippetManager.Views.DesignData;

internal class ViewModelLocator
{
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
                    new ApplySnippetCommand(InteropService.Instance), 
                    new CopySnippetCommand(InteropService.Instance)
                );

                var gitHub = tree.GetRoots().First(s => s.Title == "GitHub");
                mainViewModel.Selected.Add(gitHub);
                mainViewModel.Selected.Add(tree.GetChildren(gitHub).First());

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

        Add(
            "C# class",
            """
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace Namespace
            {
                public class Class
                {
                }
            }
            """,
            "public class Class..."
        );
        Add("Maps", "https://maps.google.com");
        Add("GitHub - dotnet - runtime", "https://github.com/dotnet/runtime");
        Add("GitHub - dotnet - aspnetcore", "https://github.com/dotnet/aspnetcore");
        Add("GitHub - dotnet - sdk", "https://github.com/dotnet/sdk");
        Add("Money", "https://app.money.neptuo.com");
        Add("Signature", $"S pozdravem{Environment.NewLine}Marek Fišera");
        return ctx;
    }
}
