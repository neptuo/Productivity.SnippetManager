using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

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
            trayIcon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); Shutdown(); };
        }

        private Configuration CreateConfiguration()
        {
            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.json");
            if (!File.Exists(filePath))
                return new Configuration();

            using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            var configuration = JsonSerializer.Deserialize<Configuration>(fileStream);
            if (configuration == null)
                return new Configuration();

            return configuration;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            if (trayIcon != null)
                trayIcon.Visible = false;
        }
    }
}
