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
                mainViewModel = new MainViewModel()
                {
                    Apply = new ApplySnippetCommand(InteropService.Instance),
                    Copy = new CopySnippetCommand(InteropService.Instance)
                };

                LoadSnippets();
            }

            return mainViewModel;
        }
    }

    private static void LoadSnippets()
    {
        void Add(string title, string text, string? description = null)
            => MainViewModel.Snippets.Add(new SnippetModel(title, text, description));

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
    }
}
