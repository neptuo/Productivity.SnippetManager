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
    private int? lastExternalProcessId;

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
        RememberLastExternalApplication();

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

        ActivateCurrentApplication();
        main.Show();
        ActivateCurrentApplication();
        main.Activate();
        main.FocusSearchText();
    }

    public void CloseMain()
        => main?.Close();

    private HelpWindow? help;

    public void OpenHelp()
    {
        RememberLastExternalApplication();

        if (help == null)
        {
            help = new HelpWindow(this);
            help.Closed += (sender, e) => { help = null; };
        }

        ActivateCurrentApplication();
        help.Show();
        ActivateCurrentApplication();
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
        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Debug.WriteLine("Clipboard is unavailable; unable to apply the selected snippet.");
            return;
        }

        string? previousText = null;
#pragma warning disable CS0618 // GetTextAsync is deprecated in favor of TryGetTextAsync in Avalonia 11.3+
        try { previousText = await clipboard.GetTextAsync(); }
        catch (Exception ex) { Debug.WriteLine($"Unable to read the current clipboard text: {ex}"); }
#pragma warning restore CS0618

        await clipboard.SetTextAsync(text);

        main?.Close();
        RestoreLastExternalApplication();

        await Task.Delay(100);

        SimulatePaste();

        await Task.Delay(200);

        if (previousText != null)
        {
            try { await clipboard.SetTextAsync(previousText); }
            catch (Exception ex) { Debug.WriteLine($"Unable to restore the previous clipboard text: {ex}"); }
        }
    }

    async void IClipboardService.SetText(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            Debug.WriteLine("Clipboard is unavailable; unable to copy the selected snippet.");
            return;
        }

        await clipboard.SetTextAsync(text);
        main?.Close();
    }

    private void RememberLastExternalApplication()
    {
        if (!OperatingSystem.IsMacOS())
            return;

        int? processId = MacOSApplication.GetFrontmostApplicationProcessId();
        if (processId.HasValue && processId.Value != Environment.ProcessId)
            lastExternalProcessId = processId.Value;
    }

    private static void ActivateCurrentApplication()
    {
        if (OperatingSystem.IsMacOS())
            MacOSApplication.ActivateCurrentProcess();
    }

    private void RestoreLastExternalApplication()
    {
        int? processId = lastExternalProcessId;
        lastExternalProcessId = null;

        if (OperatingSystem.IsMacOS() && processId.HasValue && processId.Value != Environment.ProcessId)
            MacOSApplication.ActivateProcess(processId.Value);
    }

    private IClipboard? GetClipboard()
    {
        if (main != null && TopLevel.GetTopLevel(main) is { } mainTopLevel)
            return mainTopLevel.Clipboard;

        if (help != null && TopLevel.GetTopLevel(help) is { } helpTopLevel)
            return helpTopLevel.Clipboard;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow is { } mainWindow)
                return TopLevel.GetTopLevel(mainWindow)?.Clipboard;

            if (desktop.Windows.LastOrDefault() is { } window)
                return TopLevel.GetTopLevel(window)?.Clipboard;
        }

        return null;
    }

    private static void SimulatePaste()
    {
        if (OperatingSystem.IsMacOS())
        {
            MacOSApplication.SendPasteShortcut();
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
