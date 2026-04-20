using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Neptuo.Productivity.SnippetManager.Plugins;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.Variables;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views;

namespace Neptuo.Productivity.SnippetManager;

public class Navigator : IClipboardService, ISendTextService, ITrayHostServices
{
    private readonly SnippetProviderContext snippetProviderContext;
    private readonly ISnippetProvider snippetProvider;
    private Task snippetProviderInitializeTask;
    private Action<bool> setConfigChangeEnabled;
    private readonly Action shutdown;
    private readonly Func<Configuration> getExampleConfiguration;
    private readonly Func<string> getCurrentHotkey;
    private readonly ConfigurationRepository configurationRepository;
    private readonly SnippetExpansionPipeline expansionPipeline;
    private int? lastExternalProcessId;

    public Navigator(ISnippetProvider snippetProvider, ConfigurationRepository configurationRepository, Action<bool> setConfigChangeEnabled, Action shutdown, Func<Configuration> getExampleConfiguration, Func<string> getCurrentHotkey, VariablesConfiguration? variables)
    {
        this.snippetProvider = snippetProvider;
        this.configurationRepository = configurationRepository;
        this.setConfigChangeEnabled = setConfigChangeEnabled;
        this.shutdown = shutdown;
        this.getExampleConfiguration = getExampleConfiguration;
        this.getCurrentHotkey = getCurrentHotkey;
        this.snippetProviderContext = new();
        this.snippetProviderContext.Changed += OnModelsChanged;
        this.expansionPipeline = new SnippetExpansionPipeline(
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(variables)
        );

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
        DiagnosticsLog.Info($"Navigator.OpenMain requested. Existing window: {main != null}. stickToActiveCaret={stickToActiveCaret}.");
        RememberLastExternalApplication();
        bool shouldRefreshSnippets = false;

        if (main == null)
        {
            main = new MainWindow();
            main.Closed += (sender, e) => { main = null; };
            main.ViewModel = new MainViewModel(snippetProviderContext, new ApplySnippetCommand(this, expansionPipeline), new CopySnippetCommand(this, expansionPipeline));
            UpdateWindowPositionAnchor(main, stickToActiveCaret);
            shouldRefreshSnippets = true;
        }
        else
        {
            UpdateWindowPositionAnchor(main, stickToActiveCaret);
            main.FocusSearchText();
            main.UpdatePosition();
        }

        ActivateCurrentApplication();
        main.Show();
        ActivateCurrentApplication();
        main.Activate();
        main.FocusSearchText();

        if (shouldRefreshSnippets)
            _ = UpdateSnippetsAsync(main.ViewModel);

        DiagnosticsLog.Info("Main window shown and focused.");
    }

    private static void UpdateWindowPositionAnchor(MainWindow window, bool stickToActiveCaret)
    {
        WindowPositionAnchor? anchor = null;

        if (stickToActiveCaret)
        {
            anchor = MacOSTextAnchor.TryGetForFocusedElement();
            if (anchor is { } resolvedAnchor)
            {
                DiagnosticsLog.Info($"Main window positioning will use the {resolvedAnchor.Source} anchor at ({resolvedAnchor.Bounds.X}, {resolvedAnchor.Bounds.Y}, {resolvedAnchor.Bounds.Width}, {resolvedAnchor.Bounds.Height}).");
            }
            else
            {
                DiagnosticsLog.Info("Unable to resolve a caret or focused-element anchor for the main window. Falling back to centered placement.");
            }
        }
        else
        {
            DiagnosticsLog.Info("Main window positioning requested without caret anchoring. Using centered placement.");
        }

        window.SetPositionAnchor(anchor);
    }

    public void CloseMain()
        => main?.Close();

    private HelpWindow? help;

    public void OpenHelp()
    {
        DiagnosticsLog.Info("Opening the About window.");
        RememberLastExternalApplication();

        if (help == null)
        {
            help = new HelpWindow(this);
            help.Closed += (sender, e) => { help = null; };
        }

        help.SetHotkey(getCurrentHotkey());
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

    public void OpenGitHub() => OpenUrl("https://github.com/neptuo/Productivity.SnippetManager");

    public void OpenLogs()
    {
        DiagnosticsLog.Info("Opening the diagnostics log file.");
        OpenFile(DiagnosticsLog.FilePath);
    }

    private bool isConfigurationChangedDialogOpen = false;

    public async Task<bool> ConfirmConfigurationReloadAsync()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return await Dispatcher.UIThread.InvokeAsync(ConfirmConfigurationReloadAsync);

        return await ConfirmConfigurationReloadCoreAsync();
    }

    private async Task<bool> ConfirmConfigurationReloadCoreAsync()
    {
        if (isConfigurationChangedDialogOpen)
            return false;

        try
        {
            isConfigurationChangedDialogOpen = true;
            return await ShowConfirmationDialogAsync(
                "Snippet Manager",
                "Configuration has changed. Do you want to apply changes?"
            );
        }
        finally
        {
            isConfigurationChangedDialogOpen = false;
        }
    }

    private Task<bool> ShowConfirmationDialogAsync(string title, string message)
    {
        var dialog = new Window()
        {
            Title = title,
            Width = 420,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            Topmost = true,
            ShowInTaskbar = false,
            WindowStartupLocation = GetDialogOwner() is null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner
        };

        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void CloseDialog(bool shouldReload)
        {
            if (result.TrySetResult(shouldReload))
                dialog.Close();
        }

        var yesButton = new Button()
        {
            Content = "Yes",
            MinWidth = 80,
            IsDefault = true
        };
        yesButton.Click += (_, _) => CloseDialog(true);

        var noButton = new Button()
        {
            Content = "No",
            MinWidth = 80,
            IsCancel = true
        };
        noButton.Click += (_, _) => CloseDialog(false);

        dialog.Closed += (_, _) => result.TrySetResult(false);
        dialog.Content = new StackPanel()
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock()
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children =
                    {
                        noButton,
                        yesButton
                    }
                }
            }
        };

        if (GetDialogOwner() is { } owner)
            _ = dialog.ShowDialog(owner);
        else
            dialog.Show();

        return result.Task;
    }

    private Window? GetDialogOwner()
    {
        if (main != null)
            return main;

        if (help != null)
            return help;

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.Windows.LastOrDefault(w => w.IsVisible) ?? desktop.MainWindow;

        return null;
    }

    public void Shutdown()
    {
        DiagnosticsLog.Info("Navigator shutdown requested.");
        shutdown();
    }

    #region Platform helpers

    public void OpenFile(string path)
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
            DiagnosticsLog.Error("Clipboard is unavailable; unable to apply the selected snippet.");
            return;
        }

        string? previousText = null;
#pragma warning disable CS0618 // GetTextAsync is deprecated in favor of TryGetTextAsync in Avalonia 11.3+
        try { previousText = await clipboard.GetTextAsync(); }
        catch (Exception ex) { DiagnosticsLog.Error("Unable to read the current clipboard text.", ex); }
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
            catch (Exception ex) { DiagnosticsLog.Error("Unable to restore the previous clipboard text.", ex); }
        }
    }

    async void IClipboardService.SetText(string text)
    {
        var clipboard = GetClipboard();
        if (clipboard == null)
        {
            DiagnosticsLog.Error("Clipboard is unavailable; unable to copy the selected snippet.");
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
        {
            lastExternalProcessId = processId.Value;
            DiagnosticsLog.Info($"Captured frontmost macOS application PID {processId.Value} before opening the UI.");
        }
        else
        {
            DiagnosticsLog.Info("No external macOS application PID was captured before opening the UI.");
        }
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
        {
            DiagnosticsLog.Info($"Restoring focus to macOS process {processId.Value}.");
            MacOSApplication.ActivateProcess(processId.Value);
        }
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
