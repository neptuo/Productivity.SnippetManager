using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace Neptuo.Productivity.SnippetManager
{
    public partial class App : Application
    {
        private readonly Navigator navigator = new Navigator(new CompositeSnippetProvider(new DemoSnippetProvider(), new GuidSnippetProvider()));
        private NotifyIcon? trayIcon;

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
                Text = "Snippet Manager"
            };
            trayIcon.MouseClick += (sender, e) =>
            {
                if (e.Button != MouseButtons.Right) 
                    navigator.OpenMain();
            };
            trayIcon.Visible = true;

            trayIcon.ContextMenuStrip = new ContextMenuStrip();
            trayIcon.ContextMenuStrip.Items.Add("Open").Click += (sender, e) => { navigator.OpenMain(); };
            trayIcon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => Shutdown();
        }
    }
}
