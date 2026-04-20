using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Neptuo.Productivity.SnippetManager.Plugins;
using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager;

public partial class App : Application
{
    private Configuration configuration = new();
    private ISnippetProvider provider = new CompositeSnippetProvider(Array.Empty<ISnippetProvider>());
    private Navigator navigator = null!;
    private TrayIcon? trayIcon;
    private ConfigurationWatcher? configurationWatcher;
    private Hotkey hotkey = null!;
    private readonly SnippetProviderCollection snippetProviders = new();
    private readonly ConfigurationRepository configurationRepository;
    private readonly PluginHost pluginHost;
    private readonly IReadOnlyList<ITrayMenuContributor> trayContributors;
    private bool isShuttingDown;
    private bool areResourcesDisposed;

    public App()
    {
        pluginHost = new PluginHost();
        pluginHost.AddAssembly(typeof(ClipboardPlugin).Assembly);   // Avalonia host: ClipboardPlugin
        pluginHost.AddAssembly(typeof(GuidPlugin).Assembly);        // core: Guid, Inline
        pluginHost.AddAssembly(typeof(XmlPlugin).Assembly);         // Xml plugin
        pluginHost.AddAssembly(typeof(GitHubPlugin).Assembly);      // GitHub plugin

        var container = pluginHost.Compose(snippetProviders);
        trayContributors = container.GetExportedValues<ITrayMenuContributor>().ToArray();

        configurationRepository = new ConfigurationRepository(snippetProviders);
        hotkey = new Hotkey();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            desktop.Exit += (_, _) =>
            {
                DiagnosticsLog.Info("Avalonia desktop lifetime exit event received.");
                DisposeResources();
            };

            configuration = CreateConfiguration();
            provider = snippetProviders.Create(configuration.Providers);
            navigator = CreateNavigator(() => RequestShutdown(desktop));

            trayIcon = new TrayIcon(navigator, hotkey, trayContributors);
            hotkey.Bind(navigator, configuration.General?.HotKey);
            configurationWatcher = new ConfigurationWatcher(GetConfigurationPath(), AskToReloadConfiguration);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private Navigator CreateNavigator(Action shutdown) => new Navigator(
        provider,
        configurationRepository,
        enabled => configurationWatcher?.EnableRaisingEventsFromConfigurationWatcher(enabled),
        shutdown,
        GetExampleConfiguration,
        GetCurrentHotkey,
        configuration.Variables
    );

    private void RequestShutdown(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (isShuttingDown)
        {
            DiagnosticsLog.Info("Ignoring a duplicate application shutdown request.");
            return;
        }

        isShuttingDown = true;
        DiagnosticsLog.Info("Application shutdown requested.");
        DisposeResources();

        if (!desktop.TryShutdown(0))
            DiagnosticsLog.Error("Avalonia desktop lifetime rejected the shutdown request.");
    }

    private void DisposeResources()
    {
        if (areResourcesDisposed)
            return;

        areResourcesDisposed = true;
        DiagnosticsLog.Info("Disposing application resources.");

        configurationWatcher?.Dispose();
        configurationWatcher = null;

        trayIcon?.Dispose();
        trayIcon = null;

        hotkey.Dispose();
        pluginHost.Dispose();
    }

    private Configuration GetExampleConfiguration()
    {
        var example = new Configuration();
        example.General = GeneralConfiguration.Example;
        example.Variables = VariablesConfiguration.Example;
        snippetProviders.AddExampleConfigurations(example.Providers);
        return example;
    }

    private string GetCurrentHotkey()
        => configuration.General?.HotKey ?? GeneralConfiguration.DefaultHotKey;

    private Configuration CreateConfiguration()
    {
        var filePath = GetConfigurationPath();
        if (!File.Exists(filePath))
            return new Configuration();

        try
        {
            var config = configurationRepository.Read(filePath);
            if (config == null)
                throw new JsonException("Deserialized configuration is null");

            return config;
        }
        catch (JsonException)
        {
            // On macOS, open with default text editor
            Process.Start(new ProcessStartInfo(GetConfigurationPath()) { UseShellExecute = true });
            Environment.Exit(1);
            return new Configuration();
        }
    }

    public static string GetConfigurationPath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.json");

    private async void AskToReloadConfiguration()
    {
        if (!await navigator.ConfirmConfigurationReloadAsync())
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            navigator.CloseMain();

            configuration = CreateConfiguration();
            provider = snippetProviders.Create(configuration.Providers);

            var desktop = (IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!;
            navigator = CreateNavigator(() => RequestShutdown(desktop));
            trayIcon?.Dispose();
            trayIcon = new TrayIcon(navigator, hotkey, trayContributors);
            hotkey.UnBind();
            hotkey.Bind(navigator, configuration.General?.HotKey);
        });
    }
}
