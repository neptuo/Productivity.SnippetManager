using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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
        private readonly ComponentDispatcherHotkeyCollection hotkeys = new ComponentDispatcherHotkeyCollection();
        private ConfigurationWatcher configurationWatcher;
        private (Key key, ModifierKeys modifiers)? hotkey;
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
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            configuration = CreateConfiguration();
            provider = snippetProviders.Create(configuration);
            navigator = CreateNavigator();
            trayIcon = new TrayIcon(navigator);

            BindHotkey();
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

        private void BindHotkey()
        {
            var key = Key.V;
            var modifiers = ModifierKeys.Control | ModifierKeys.Shift;
            if (!String.IsNullOrEmpty(configuration.General?.HotKey) && !TryParseHotKey(configuration.General.HotKey, out key, out modifiers))
            {
                MessageBox.Show("Error in hotkey configuration", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            hotkey = (key, modifiers);

            try
            {
                hotkeys.Add(key, modifiers, (_, _) => navigator.OpenMain());
            }
            catch (Win32Exception)
            {
                MessageBox.Show("The configured hotkey is probably taken by other application. Edit your configuration.", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                navigator.OpenConfiguration();
                Dispatcher.Invoke(() => Shutdown());
            }
        }

        private bool TryParseHotKey(string hotKey, out Key key, out ModifierKeys modifiers)
        {
            modifiers = ModifierKeys.None;
            key = Key.None;

            string[] parts = hotKey.Split('+', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i < parts.Length - 1)
                    {
                        if (Enum.TryParse<ModifierKeys>(parts[i], out var mod))
                        {
                            if (i == 0)
                                modifiers = mod;
                            else
                                modifiers |= mod;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Enum.TryParse<Key>(parts[i], out var k))
                            key = k;
                        else
                            return false;
                    }
                }
            }

            return true;
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
                    provider = snippetProviders.Create(configuration);
                    navigator = CreateNavigator();

                    if (hotkey != null && configuration.General?.HotKey != oldHotKey)
                    {
                        hotkeys.Remove(hotkey.Value.key, hotkey.Value.modifiers);
                        BindHotkey();
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
