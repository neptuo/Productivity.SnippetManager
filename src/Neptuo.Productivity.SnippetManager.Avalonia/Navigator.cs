using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Threading;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views;

namespace Neptuo.Productivity.SnippetManager;

public class Navigator : IClipboardService, ISendTextService
{
    private readonly SnippetProviderContext snippetProviderContext;
    private readonly ISnippetProvider snippetProvider;
    private Task snippetProviderInitializeTask;
    private Action<bool> setConfigChangeEnabled;
    private readonly Action shutdown;
    private readonly Func<string> getXmlSnippetsPath;
    private readonly Func<Configuration> getExampleConfiguration;
    private readonly ConfigurationRepository configurationRepository;

    public Navigator(ISnippetProvider snippetProvider, ConfigurationRepository configurationRepository, Action<bool> setConfigChangeEnabled, Action shutdown, Func<string> getXmlSnippetsPath, Func<Configuration> getExampleConfiguration)
    {
        this.snippetProvider = snippetProvider;
        this.configurationRepository = configurationRepository;
        this.setConfigChangeEnabled = setConfigChangeEnabled;
        this.shutdown = shutdown;
        this.getXmlSnippetsPath = getXmlSnippetsPath;
        this.getExampleConfiguration = getExampleConfiguration;
        this.snippetProviderContext = new();
        this.snippetProviderContext.Changed += OnModelsChanged;

        snippetProviderInitializeTask = snippetProvider.InitializeAsync(snippetProviderContext);
    }

    private void OnModelsChanged()
    {
        if (main != null)
            Dispatcher.UIThread.InvokeAsync(() => main?.Search());
    }

    private MainWindow? main;

    public void OpenMain(bool stickToActiveCaret = true)
    {
        if (main == null)
        {
            main = new MainWindow();
            main.Closed += (sender, e) => { main = null; };
            main.ViewModel = new MainViewModel(snippetProviderContext, new ApplySnippetCommand(this), new CopySnippetCommand(this));
            // On macOS, caret detection is not straightforward — center the window
            main.SetStickPoint(null);

            _ = UpdateSnippetsAsync(main.ViewModel);
        }
        else
        {
            main.FocusSearchText();
            main.UpdatePosition();
        }

        main.Show();
        main.Activate();
        main.FocusSearchText();
    }

    public void CloseMain()
        => main?.Close();

    private HelpWindow? help;

    public void OpenHelp()
    {
        if (help == null)
        {
            help = new HelpWindow(this);
            help.Closed += (sender, e) => { help = null; };
        }

        help.Show();
        help.Activate();
    }

    private async Task UpdateSnippetsAsync(MainViewModel viewModel)
    {
        if (!snippetProviderInitializeTask.IsCompleted)
        {
            main?.Search();
            await snippetProviderInitializeTask;

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
            // For cross-platform: just create the configuration
            try
            {
                setConfigChangeEnabled(false);
                var example = getExampleConfiguration();
                configurationRepository.Write(filePath, example);
            }
            finally
            {
                setConfigChangeEnabled(true);
            }
        }

        OpenFile(filePath);
    }

    public void OpenXmlSnippets()
    {
        string filePath = getXmlSnippetsPath();
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, """
                <?xml version="1.0" encoding="utf-8" ?>
                <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
                  <Snippet Title="Greet" Text="Hello, World!" />
                  <Snippet Title="Weather Forecast" Priority="High">
                <![CDATA[Prague 22,
                London 18,
                New York 25]]></Snippet>
                </Snippets>
                """);
        }

        OpenFile(filePath);
    }

    public void OpenGitHub() => OpenUrl("https://github.com/neptuo/Productivity.SnippetManager");

    private bool isConfigurationChangedDialogOpen = false;

    public bool ConfirmConfigurationReload()
    {
        if (isConfigurationChangedDialogOpen)
            return false;

        // Auto-reload on config change (no dialog on macOS for simplicity)
        return true;
    }

    public void Shutdown()
        => shutdown();

    #region Platform helpers

    private static void OpenFile(string path)
    {
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    #endregion

    #region Services

    async void ISendTextService.Send(string text)
    {
        main?.Close();

        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            // Store current clipboard
            string? previousText = null;
#pragma warning disable CS0618 // GetTextAsync is deprecated in favor of TryGetTextAsync in Avalonia 11.3+
            try { previousText = await clipboard.GetTextAsync(); }
            catch { /* clipboard may be empty */ }
#pragma warning restore CS0618

            // Set the snippet text
            await clipboard.SetTextAsync(text);

            await Task.Delay(100);

            // Simulate Cmd+V on macOS
            SimulatePaste();

            await Task.Delay(200);

            // Restore previous clipboard content
            if (previousText != null)
            {
                try { await clipboard.SetTextAsync(previousText); } catch { }
            }
        }
    }

    void IClipboardService.SetText(string text)
    {
        main?.Close();

        var clipboard = GetClipboard();
        if (clipboard != null)
        {
            _ = clipboard.SetTextAsync(text);
        }
    }

    private static IClipboard? GetClipboard()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var topLevel = desktop.MainWindow ?? TopLevel.GetTopLevel(desktop.Windows.FirstOrDefault());
            return topLevel?.Clipboard;
        }
        return null;
    }

    private static void SimulatePaste()
    {
        if (OperatingSystem.IsMacOS())
        {
            // Use osascript to simulate Cmd+V on macOS
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-e 'tell application \"System Events\" to keystroke \"v\" using command down'",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });
            }
            catch
            {
                // Paste simulation failed; user can paste manually
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdotool",
                    Arguments = "key ctrl+v",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }
            catch
            {
                // xdotool not available
            }
        }
    }

    #endregion
}
