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
using Point = System.Drawing.Point;

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
            
            _ = UpdateSnippetsAsync(main.ViewModel);
        }
        else
        {
            PositionWindowToCaret(main);
            main.FocusSearchText();
        }

        PositionWindowToCaret(main);
        main.Show();
        main.Activate();
        main.FocusSearchText();
    }

    public void CloseMain()
        => main?.Close();

    private void PositionWindowToCaret(Window wnd)
    {
        var caret = CaretPosition.Find();
        if (caret == null)
        {
            MoveToActiveScreen(wnd);
            return;
        }

        wnd.Left = caret.Value.Right;
        wnd.Top = caret.Value.Bottom;
        EnsureWindowIsVisible(wnd);
    }

    private void MoveToActiveScreen(Window wnd)
    {
        var activeHandle = Win32.GetForegroundWindow();
        var screen = Screen.FromHandle(activeHandle);

        wnd.Left = screen.Bounds.Left + (screen.Bounds.Width - wnd.ActualWidth) / 2;
        wnd.Top = screen.Bounds.Top + (screen.Bounds.Height - wnd.ActualHeight) / 2;
    }

    private static void EnsureWindowIsVisible(Window wnd)
    {
        var wndPoint = new Point((int)wnd.Left, (int)wnd.Top);
        Screen activeScreen = Screen.FromPoint(wndPoint);

        var wndRight = wnd.Left + wnd.Width;
        var screenRight = activeScreen.WorkingArea.X + activeScreen.WorkingArea.Width;
        if (wndRight > screenRight)
            wnd.Left = screenRight - wnd.Width;

        var wndBottom = wnd.Top + wnd.Height;
        var screenBottom = activeScreen.WorkingArea.Y + activeScreen.WorkingArea.Height;
        if (wndBottom > screenBottom)
            wnd.Top = screenBottom - wnd.Height;
    }

    private async Task UpdateSnippetsAsync(MainViewModel viewModel)
    {
        if (!isSnipperProviderInitialized)
        {
            if (snipperProviderInitializeTask == null)
                snipperProviderInitializeTask = snippetProvider.InitializeAsync(snippetProviderContext);
            else
                main?.ViewModel.RefreshSearch();

            await snipperProviderInitializeTask;
            snipperProviderInitializeTask = null;
            isSnipperProviderInitialized = true;

            // Main lost focus and is closed.
            if (main == null)
                return;
        }

        await snippetProvider.UpdateAsync(snippetProviderContext);
        main?.Search();
    }

    private void OnModelsChanged()
    {
        if (main != null)
            DispatcherHelper.Run(main.Dispatcher, () => main.ViewModel.RefreshSearch());
    }

    #region Services

    async void ISendTextService.Send(string text)
    {
        var scope = new ClipboardScope();
        try
        {
            main?.Close();

            Clipboard.SetText(text);
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

    class Win32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}
