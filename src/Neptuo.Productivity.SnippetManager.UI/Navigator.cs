using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views;
using Neptuo.Productivity.SnippetManager.Views.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using Clipboard = System.Windows.Forms.Clipboard;
using Point = System.Drawing.Point;

namespace Neptuo.Productivity.SnippetManager;

public class Navigator : IClipboardService, ISendTextService
{
    private readonly SnippetProviderContext snippetProviderContext = new();
    private readonly ISnippetProvider snippetProvider;
    private bool isSnipperProviderInitialized = false;

    public Navigator(ISnippetProvider snippetProvider)
    {
        this.snippetProvider = snippetProvider;
    }

    private MainWindow? main;

    public void OpenMain()
    {
        if (main == null)
        {
            main = new MainWindow();
            main.SourceInitialized += (sender, e) =>
            {
                PositionWindowToCaret(main);
                main.FocusSearchText();
            };
            main.Closed += (sender, e) => { main = null; };

            var viewModel = new MainViewModel()
            {
                Apply = new ApplySnippetCommand(this),
                Copy = new CopySnippetCommand(this)
            };

            main.DataContext = viewModel;

            _ = UpdateSnippetsAsync(viewModel);
        }
        else
        {
            PositionWindowToCaret(main);
            main.FocusSearchText();
        }

        main.Activate();
        main.Show();
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
            await snippetProvider.InitializeAsync(snippetProviderContext);
            isSnipperProviderInitialized = true;
        }

        await snippetProvider.UpdateAsync(snippetProviderContext);
        viewModel.Snippets.AddRange(snippetProviderContext.Models);
        main?.Search();
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

    #endregion

    class Win32
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();
    }
}
