using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views;
using Neptuo.Productivity.SnippetManager.Views.Controls;
using Neptuo.Windows.Threading;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Clipboard = System.Windows.Forms.Clipboard;
using MessageBox = System.Windows.MessageBox;

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
            DispatcherHelper.Run(main.Dispatcher, () => main?.Search());
    }

    private MainWindow? main;

    public void OpenMain(bool stickToActiveCaret = true)
    {
        if (main == null)
        {
            main = new MainWindow();
            main.Closed += (sender, e) => { main = null; };
            main.ViewModel = new MainViewModel(snippetProviderContext, new ApplySnippetCommand(this), new CopySnippetCommand(this));
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

    private void UpdateWindowStickPointToCaret(MainWindow wnd, bool stickToActiveCaret)
    {
        var caret = stickToActiveCaret ? CaretPosition.Find() : null;
        wnd.SetStickPoint(caret);
    }

    private async Task UpdateSnippetsAsync(MainViewModel viewModel)
    {
        if (!snippetProviderInitializeTask.IsCompleted)
        {
            // To show up to date currently available snippets
            main?.Search();

            await snippetProviderInitializeTask;

            // Main lost focus and is closed
            if (main == null)
                return;
        }

        await snippetProvider.UpdateAsync(snippetProviderContext);

        if (main != null)
        {
            main.Search();

            // Hide initialization after update completes
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

                    var example = getExampleConfiguration();
                    configurationRepository.Write(filePath, example);
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

    public void OpenXmlSnippets()
    {
        string filePath = getXmlSnippetsPath();
        if (!File.Exists(filePath))
        {
            var result = MessageBox.Show("XML snippets file doesn't exist yet. Do you want to create one?", "Snippet Manager", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                File.WriteAllText(filePath, """
                    <?xml version="1.0" encoding="utf-8" ?>
                    <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
                      <Snippet Title="Greet" Text="Hello, World!" />
                      <Snippet Title="Wheather Forecast" Priority="High">
                    <![CDATA[Prague 22,
                    London 18,
                    New York 25]]></Snippet>
                    </Snippets>
                    """);
            }
            else
            {
                return;
            }
        }

        // Duplicated in App.xaml
        Process.Start("explorer", filePath);
    }

    public void OpenGitHub() => Process.Start(new ProcessStartInfo()
    {
        FileName = "https://github.com/neptuo/Productivity.SnippetManager",
        UseShellExecute = true
    });

    private bool isConfigurationChangedDialogOpen;

    public bool ConfirmConfigurationReload()
    {
        if (isConfigurationChangedDialogOpen)
            return false;

        try
        {
            isConfigurationChangedDialogOpen = true;
            return MessageBox.Show("Configuration has changed. Do you want to apply changes?", "Snippet Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }
        finally
        {
            isConfigurationChangedDialogOpen = false;
        }
    }

    public void Shutdown() 
        => shutdown();

    #region Services

    async void ISendTextService.Send(string text)
    {
        var scope = new ClipboardScope();
        try
        {
            bool isCtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

            main?.Close();

            Clipboard.SetText(text);

            await Task.Delay(100);
            
            if (isCtrlDown)
                await WaitForCtrlReleaseAsync();

            SendKeys.SendWait("+{INSERT}");

            if (isCtrlDown)
                SendKeys.SendWait("{ENTER}");

            await Task.Delay(100);
        }
        finally
        {
            scope.Restore();
        }
    }

    private async Task WaitForCtrlReleaseAsync()
    {
        // Wait for Ctrl key release with timeout
        int timeout = 5000; // 5 seconds timeout
        int stepDelay = 50;
        int elapsed = 0;

        while ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && elapsed < timeout)
        {
            await Task.Delay(stepDelay);
            elapsed += stepDelay;
        }
    }

    void IClipboardService.SetText(string text)
    {
        main?.Close();
        Clipboard.SetText(text);
    }

    #endregion
}
