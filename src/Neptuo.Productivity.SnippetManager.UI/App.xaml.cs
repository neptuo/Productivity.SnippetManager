using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public App()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            snippetProviders.AddConfigChangeTracking<ProviderConfiguration>("Clipboard", c => new ClipboardSnippetProvider(), true);
            snippetProviders.AddConfigChangeTracking<ProviderConfiguration>("Guid", c => new GuidSnippetProvider(), true);
            snippetProviders.AddConfigChangeTracking<XmlConfiguration>("Xml", c => new XmlSnippetProvider(c), true);
            snippetProviders.AddConfigChangeTracking<GitHubConfiguration>("GitHub", c => new GitHubSnippetProvider(c));
            snippetProviders.AddNotNullConfiguration<InlineSnippetConfiguration>("Snippets", c => new InlineSnippetProvider(c));
            configurationRepository = new ConfigurationRepository(snippetProviders);
            hotkey = new Hotkey();

            Win32.RegisterApplicationRestart("/restart", Win32.RestartRestrictions.None);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            configuration = CreateConfiguration();
            provider = snippetProviders.Create(configuration.Providers);
            navigator = CreateNavigator();
            trayIcon = new TrayIcon(navigator, hotkey);

            hotkey.Bind(navigator, Dispatcher, configuration.General?.HotKey);
            configurationWatcher = new ConfigurationWatcher(GetConfigurationPath(), AskToReloadConfiguration);
        }

        private Navigator CreateNavigator() => new Navigator(
            provider,
            configurationRepository,
            enabled => configurationWatcher.EnableRaisingEventsFromConfigurationWatcher(enabled),
            Shutdown,
            GetXmlConfigurationPath,
            GetExampleConfiguration
        );

        private string GetXmlConfigurationPath() 
            => (configuration.Providers.GetValueOrDefault("Xml") as XmlConfiguration ?? XmlConfiguration.Example).GetFilePathOrDefault();

        private Configuration GetExampleConfiguration()
        {
            var example = new Configuration();
            example.General = GeneralConfiguration.Example;
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

                    string? oldHotKey = configuration.General?.HotKey ?? GeneralConfiguration.Example.HotKey;

                    configuration = CreateConfiguration();
                    provider = snippetProviders.Create(configuration.Providers);
                    navigator = CreateNavigator();

                    if (hotkey != null && configuration.General?.HotKey != oldHotKey)
                    {
                        hotkey.UnBind();
                        hotkey.Bind(navigator, Dispatcher, configuration.General?.HotKey);
                    }
                });
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            trayIcon.Dispose();
            configurationWatcher?.Dispose();
        }
    }
}
