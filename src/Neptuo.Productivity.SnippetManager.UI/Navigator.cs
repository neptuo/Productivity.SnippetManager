using Neptuo.Observables.Collections;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views;
using Neptuo.Productivity.SnippetManager.Views.Controls;
using Neptuo.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace Neptuo.Productivity.SnippetManager;

public class Navigator : IClipboardService, ISendTextService
{
    private readonly ObservableCollection<SnippetModel> allSnippets;
    private readonly SnippetProviderContext snippetProviderContext;
    private readonly ISnippetProvider snippetProvider;
    private readonly Dispatcher dispatcher;
    private bool isSnipperProviderInitialized = false;
    private Task? snipperProviderInitializeTask;

    public Navigator(ISnippetProvider snippetProvider, Dispatcher dispatcher)
    {
        this.snippetProvider = snippetProvider;
        this.dispatcher = dispatcher;
        this.allSnippets = new();
        this.snippetProviderContext = new(allSnippets);
        this.snippetProviderContext.Changed += OnModelsChanged;
    }

    private MainWindow? main;

    public void OpenMain()
    {
        if (main == null)
        {
            main = new MainWindow();
            main.Closed += (sender, e) => { main = null; };
            main.ViewModel = new MainViewModel(allSnippets, new ApplySnippetCommand(this), new CopySnippetCommand(this));
            UpdateWindowStickPointToCaret(main);

            _ = UpdateSnippetsAsync(main.ViewModel);
        }
        else
        {
            UpdateWindowStickPointToCaret(main);
            main.FocusSearchText();
            main.UpdatePosition();
        }

        main.Show();
        main.Activate();
        main.FocusSearchText();
    }

    public void CloseMain()
        => main?.Close();

    private void UpdateWindowStickPointToCaret(MainWindow wnd)
    {
        var caret = CaretPosition.Find();
        wnd.SetStickPoint(caret);
    }

    private async Task UpdateSnippetsAsync(MainViewModel viewModel)
    {
        if (!isSnipperProviderInitialized)
        {
            if (snipperProviderInitializeTask == null)
                snipperProviderInitializeTask = snippetProvider.InitializeAsync(snippetProviderContext);
            else
                main?.Search();

            await snipperProviderInitializeTask;
            snipperProviderInitializeTask = null;
            isSnipperProviderInitialized = true;

            // Main lost focus and is closed.
            if (main == null)
                return;

        }

        await snippetProvider.UpdateAsync(snippetProviderContext);

        if (main != null)
        {
            main.Search();
            main.ViewModel.IsInitializing = false;
        }
    }

    private void OnModelsChanged()
    {
        if (main != null)
            DispatcherHelper.Run(main.Dispatcher, () => main.Search());
    }

    #region Services

    async void ISendTextService.Send(string text)
    {
        var scope = new ClipboardScope();
        try
        {
            main?.Close();

            Clipboard.SetText(text);

            await Task.Delay(100);
            SendKeys.SendWait("^{v}");
            await Task.Delay(100);
        }
        finally
        {
            scope.Restore();
        }
    }

    void IClipboardService.SetText(string text)
    {
        main?.Close();
        Clipboard.SetText(text);
    }

    public void OpenConfiguration()
    {
        string filePath = App.GetConfigurationPath();
        if (!File.Exists(filePath))
        {
            var result = MessageBox.Show("Configuration file does't exist yet. Do you want to create one?", "Snippet Manager", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                File.WriteAllText(filePath, JsonSerializer.Serialize(Configuration.Example, options: options));
            }
            else
            {
                return;
            }
        }

        Process.Start("explorer", filePath);
    }

    #endregion
}
