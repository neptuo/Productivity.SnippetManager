using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using Neptuo.Productivity.SnippetManager.Plugins;
using Neptuo.Productivity.SnippetManager.Variables;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Neptuo.Productivity.SnippetManager
{
    /// <summary>
    /// Most of the state is initialized in two places
    /// - Standard startup
    /// - OnConfigurationFileChanged
    /// </summary>
    public partial class App : Application
    {
        private Configuration configuration;
        private ISnippetProvider provider;
        private Navigator navigator;
        private TrayIcon trayIcon;
        private ConfigurationWatcher configurationWatcher;
        private Hotkey hotkey;
        private readonly SnippetProviderCollection snippetProviders = new SnippetProviderCollection();
        private readonly ConfigurationRepository configurationRepository;
        private readonly PluginHost pluginHost;
        private readonly IReadOnlyList<ITrayMenuContributor> trayContributors;
        private readonly HostServices hostServices = new HostServices();
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public App()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            pluginHost = new PluginHost();
            pluginHost.AddAssembly(typeof(ClipboardPlugin).Assembly);   // WPF host: ClipboardPlugin
            pluginHost.AddAssembly(typeof(GuidPlugin).Assembly);        // core: Guid, Inline
            pluginHost.AddAssembly(typeof(XmlPlugin).Assembly);         // Xml plugin
            pluginHost.AddAssembly(typeof(GitHubPlugin).Assembly);      // GitHub plugin
            pluginHost.AddExportedValue<INavigator>(hostServices);

            var container = pluginHost.Compose(snippetProviders);
            trayContributors = container.GetExportedValues<ITrayMenuContributor>().ToArray();

            configurationRepository = new ConfigurationRepository(snippetProviders);
            hotkey = new Hotkey();

#if !DEBUG
            Win32.RegisterApplicationRestart("/restart", Win32.RestartRestrictions.None);
#endif
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            configuration = CreateConfiguration();
            provider = snippetProviders.Create(configuration.Providers);
            navigator = CreateNavigator();
            hostServices.Target = navigator;
            trayIcon = new TrayIcon(navigator, hotkey, trayContributors);

            hotkey.Bind(navigator, Dispatcher, configuration.General?.HotKey);
            configurationWatcher = new ConfigurationWatcher(GetConfigurationPath(), AskToReloadConfiguration);
        }

        private Navigator CreateNavigator() => new Navigator(
            provider,
            configurationRepository,
            enabled => configurationWatcher.EnableRaisingEventsFromConfigurationWatcher(enabled),
            Shutdown,
            GetExampleConfiguration,
            configuration.Variables
        );

        private Configuration GetExampleConfiguration()
        {
            var example = new Configuration();
            example.General = GeneralConfiguration.Example;
            example.Variables = VariablesConfiguration.Example;
            snippetProviders.AddExampleConfigurations(example.Providers);

            return example;
        }

        private Configuration CreateConfiguration()
        {
            var filePath = GetConfigurationPath();
            if (!File.Exists(filePath))
                return new Configuration();

            try
            {
                var configuration = configurationRepository.Read(filePath);
                if (configuration == null)
                    throw new JsonException("Deserialized configuration is null");

                return configuration;
            }
            catch (JsonException)
            {
                MessageBox.Show("The configuration is not valid. Opening the configuration file and exiting...", "Snippet Manager");
                // Duplicated in Navigator.cs/OpenConfiguration
                Process.Start("explorer", GetConfigurationPath());
                Shutdown(1);

                return new Configuration();
            }
        }

        public static string GetConfigurationPath()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.json");

        private void AskToReloadConfiguration()
        {
            if (navigator.ConfirmConfigurationReload())
            {
                Dispatcher.Invoke(() =>
                {
                    navigator.CloseMain();

                    configuration = CreateConfiguration();
                    provider = snippetProviders.Create(configuration.Providers);
                    navigator = CreateNavigator();
                    hostServices.Target = navigator;

                    trayIcon.Dispose();
                    trayIcon = new TrayIcon(navigator, hotkey, trayContributors);

                    hotkey.UnBind();
                    hotkey.Bind(navigator, Dispatcher, configuration.General?.HotKey);
                });
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            trayIcon.Dispose();
            configurationWatcher?.Dispose();
            pluginHost.Dispose();
        }
    }
}
