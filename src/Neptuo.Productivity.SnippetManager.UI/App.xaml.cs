using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
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

            navigator = new Navigator(new CompositeSnippetProvider(providers));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var hotkeys = new ComponentDispatcherHotkeyCollection();
            hotkeys.Add(Key.V, ModifierKeys.Control | ModifierKeys.Shift, (_, _) =>
            {
                navigator.OpenMain();
            });

            CreateTrayIcon();
        }

        private void CreateTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule!.FileName!),
                Text = "Snippet Manager",
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
                string filePath = GetConfigurationPath();
                if (!File.Exists(filePath))
                {
                    var result = MessageBox.Show("Configuration file does't exist yet. Do you want to create one?", "Snippet Manager", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        File.WriteAllText(filePath, JsonSerializer.Serialize(Configuration.Example, options: options));
                    }
                    else
                    {
                        return;
                    }
                }

                Process.Start("explorer", filePath);
            };
            trayIcon.ContextMenuStrip.Items.Add("GitHub repository").Click += (sender, e) =>
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

        private static string GetConfigurationPath()
            => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.json");

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (trayIcon != null)
                trayIcon.Visible = false;
        }
    }
}
