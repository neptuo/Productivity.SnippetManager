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

    private static async void LoadSnippets() 
        => MainViewModel.Snippets.AddRange(await new DemoSnippetProvider().GetAsync());
}
