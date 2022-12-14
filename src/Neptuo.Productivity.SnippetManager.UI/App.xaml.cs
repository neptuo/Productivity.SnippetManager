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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Neptuo.Productivity.SnippetManager
{
    public partial class App : Application
    {
        private readonly Configuration configuration;
        private readonly Navigator navigator;
        private NotifyIcon? trayIcon;

        public App()
        {
            configuration = CreateConfiguration();

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

            navigator = new Navigator(new CompositeSnippetProvider(providers), Dispatcher);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var hotkeys = new ComponentDispatcherHotkeyCollection();

            var key = Key.V;
            var modifiers = ModifierKeys.Control | ModifierKeys.Shift;
            if (!String.IsNullOrEmpty(configuration.General?.HotKey) && !TryParseHotKey(configuration.General.HotKey, out key, out modifiers))
            {
                MessageBox.Show("Error in hotkey configuration", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            try
            {
                hotkeys.Add(key, modifiers, (_, _) =>
                {
                    navigator.OpenMain();
                });
            }
            catch (Win32Exception)
            {
                MessageBox.Show("The configured hotkey is probably taken by other application. Edit your configuration.", "Snippet Manager", MessageBoxButton.OK, MessageBoxImage.Error);
                navigator.OpenConfiguration();
                Shutdown();
            }

            CreateTrayIcon();
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

        private void CreateTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule!.FileName!),
                Text = "Snippet Manager " + ApplicationVersion.GetDisplayString(),
                Visible = true
            };
            trayIcon.MouseClick += (sender, e) =>
            {
                if (e.Button != MouseButtons.Right)
                    navigator.OpenMain();
            };

            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Open").Click += (sender, e) => { navigator.OpenMain(); };
            trayIcon.ContextMenuStrip.Items.Add("Configuration").Click += (sender, e) =>
            {
                navigator.OpenConfiguration();
            };
            trayIcon.ContextMenuStrip.Items.Add("About").Click += (sender, e) =>
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "https://github.com/neptuo/Productivity.SnippetManager",
                    UseShellExecute = true
                });
            };
            trayIcon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); Shutdown(); };
        }

        private Configuration CreateConfiguration()
        {
            var filePath = GetConfigurationPath();
            if (!File.Exists(filePath))
                return new Configuration();

            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            var configuration = JsonSerializer.Deserialize<Configuration>(fileStream);
            if (configuration == null)
                return new Configuration();

            return configuration;
        }

        public static string GetConfigurationPath()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.json");

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (trayIcon != null)
                trayIcon.Visible = false;
        }
    }
}
