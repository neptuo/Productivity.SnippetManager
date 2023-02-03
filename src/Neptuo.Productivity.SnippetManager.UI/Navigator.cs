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
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace Neptuo.Productivity.SnippetManager;

public class Navigator : IClipboardService, ISendTextService
{
    private readonly ObservableCollection<SnippetModel> allSnippets;
    private readonly SnippetProviderContext snippetProviderContext;
    private readonly ISnippetProvider snippetProvider;
    private bool isSnipperProviderInitialized = false;
    private Task? snipperProviderInitializeTask;
    private Action<bool> setConfigChangeEnabled;
    private readonly Action shutdown;

    public Navigator(ISnippetProvider snippetProvider, Action<bool> setConfigChangeEnabled, Action shutdown)
    {
        this.snippetProvider = snippetProvider;
        this.setConfigChangeEnabled = setConfigChangeEnabled;
        this.shutdown = shutdown;
        this.allSnippets = new();
        this.snippetProviderContext = new(allSnippets);
        this.snippetProviderContext.Changed += OnModelsChanged;
    }

    private void OnModelsChanged()
    {
        if (main != null)
            DispatcherHelper.Run(main.Dispatcher, () => main.Search());
    }

    private MainWindow? main;

    public void OpenMain(bool stickToActiveCaret = true)
    {
        if (main == null)
        {
            main = new MainWindow();
            main.Closed += (sender, e) => { main = null; };
            main.ViewModel = new MainViewModel(allSnippets, new ApplySnippetCommand(this), new CopySnippetCommand(this));
            UpdateWindowStickPointToCaret(main, stickToActiveCaret);

            _ = UpdateSnippetsAsync(main.ViewModel);
        }
        else
        {
            UpdateWindowStickPointToCaret(main, stickToActiveCaret);
            main.FocusSearchText();
            main.UpdatePosition();
        }

        main.Show();
        main.Activate();
        main.FocusSearchText();
    }

    public void CloseMain()
        => main?.Close();

    private void UpdateWindowStickPointToCaret(MainWindow wnd, bool stickToActiveCaret)
    {
        var caret = stickToActiveCaret ? CaretPosition.Find() : null;
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

    public void OpenConfiguration()
    {
        string filePath = App.GetConfigurationPath();
        if (!File.Exists(filePath))
        {
            var result = MessageBox.Show("Configuration file doesn't exist yet. Do you want to create one?", "Snippet Manager", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    setConfigChangeEnabled(false);
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };
                    File.WriteAllText(filePath, JsonSerializer.Serialize(Configuration.Example, options: options));
                }
                finally
                {
                    setConfigChangeEnabled(true);
                }
            }
            else
            {
                return;
            }
        }

        // Duplicated in App.xaml
        Process.Start("explorer", filePath);
    }

    public void Shutdown() 
        => shutdown();

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

    #endregion
}
