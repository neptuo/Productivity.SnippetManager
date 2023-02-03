﻿using Neptuo.Productivity.SnippetManager.Views;
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
using System.Security.Policy;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Windows.ApplicationModel.Core;
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
        private CompositeSnippetProvider provider;
        private Navigator navigator;
        private TrayIcon trayIcon;
        private ComponentDispatcherHotkeyCollection hotkeys;
        private FileSystemWatcher? configurationWatcher;
        private (Key key, ModifierKeys modifiers)? hotkey;

        public App()
        {
            hotkeys = new ComponentDispatcherHotkeyCollection();
        }

        private void EnableRaisingEventsFromConfigurationWatcher(bool enabled)
        {
            if (configurationWatcher != null)
                configurationWatcher.EnableRaisingEvents = enabled;
        }

        private CompositeSnippetProvider CreateProvider()
        {
            List<ISnippetProvider> providers = new List<ISnippetProvider>();

            if (configuration.Clipboard == null || configuration.Clipboard.IsEnabled)
                providers.Add(new ClipboardSnippetProvider());

            if (configuration.Guid == null || configuration.Guid.IsEnabled)
                providers.Add(new GuidSnippetProvider());

            if (configuration.Xml == null || configuration.Xml.IsEnabled)
                providers.Add(new XmlSnippetProvider(configuration.Xml ?? new XmlConfiguration()));

            if (configuration.GitHub == null || configuration.GitHub.IsEnabled)
                providers.Add(new GitHubSnippetProvider(configuration.GitHub ?? new GitHubConfiguration()));

            // Useful only is we have configured snippets
            if (configuration.Snippets != null)
                providers.Add(new InlineSnippetProvider(configuration.Snippets));

            var provider = new CompositeSnippetProvider(providers);
            return provider;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            configuration = CreateConfiguration();
            provider = CreateProvider();
            navigator = new Navigator(
                provider,
                EnableRaisingEventsFromConfigurationWatcher,
                Shutdown
            );
            trayIcon = new TrayIcon(navigator);

            BindHotkey();
            CreateConfigurationWatcher();
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
                using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                var configuration = JsonSerializer.Deserialize<Configuration>(fileStream);
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

        private void CreateConfigurationWatcher()
        {
            configurationWatcher = new FileSystemWatcher(Path.GetDirectoryName(GetConfigurationPath())!, "*.json");
            configurationWatcher.Changed += OnConfigurationFileChanged;
            configurationWatcher.Deleted += OnConfigurationFileChanged;
            configurationWatcher.Created += OnConfigurationFileChanged;
            configurationWatcher.Renamed += OnConfigurationFileRenamed;
            configurationWatcher.EnableRaisingEvents = true;
        }

        private CancellationTokenSource? cts;

        private async void OnConfigurationFileRenamed(object sender, RenamedEventArgs e)
        {
            if (e.OldFullPath == GetConfigurationPath())
                await ReloadConfigurationWithConfirmationAsync();
            else
                OnConfigurationFileChanged(sender, e);
        }

        private async void OnConfigurationFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == GetConfigurationPath())
                await ReloadConfigurationWithConfirmationAsync();
        }

        private async Task ReloadConfigurationWithConfirmationAsync()
        {
            if (cts != null)
                cts.Cancel();

            cts = new CancellationTokenSource();

            bool isCancelled = await WaitWithCancellationAsync(cts.Token);
            cts = null;

            if (isCancelled)
                return;

            if (MessageBox.Show("Configuration has changed. Do you want to apply changes?", "Snippet Manager", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                Dispatcher.Invoke(() =>
                {
                    navigator.CloseMain();

                    string? oldHotKey = configuration.General?.HotKey ?? Configuration.Example.General?.HotKey;

                    configuration = CreateConfiguration();
                    provider = CreateProvider();
                    navigator = new Navigator(provider, EnableRaisingEventsFromConfigurationWatcher, Shutdown);

                    if (hotkey != null && configuration.General?.HotKey != oldHotKey)
                    {
                        hotkeys.Remove(hotkey.Value.key, hotkey.Value.modifiers);
                        BindHotkey();
                    }
                });
            }
        }

        private async Task<bool> WaitWithCancellationAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(2 * 1000);
            return cancellationToken.IsCancellationRequested;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            trayIcon.Dispose();
            configurationWatcher?.Dispose();
        }
    }
}
