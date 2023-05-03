using Neptuo.Observables.Collections;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

                MainViewModel.Selected.Add(new SnippetModel("Selected grandparent", "Selected grandparent"));
                MainViewModel.Selected.Add(new SnippetModel("Selected parent", "Selected parent"));


                MainViewModel.IsInitializing = false;
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
        Add("Money", "https://app.money.neptuo.com");
        Add("Signature", $"S pozdravem{Environment.NewLine}Marek Fišera");
        return ctx;
    }
}
