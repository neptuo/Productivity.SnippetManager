using Neptuo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public class TrayIcon : IDisposable
{
    private readonly Navigator navigator;
    private readonly NotifyIcon icon;

    public TrayIcon(Navigator navigator)
    {
        Ensure.NotNull(navigator, "navigator");
        this.navigator = navigator;

        icon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule!.FileName!),
            Text = "Snippet Manager",
            Visible = true
        };
        icon.MouseClick += (sender, e) =>
        {
            if (e.Button != MouseButtons.Right)
                navigator.OpenMain(stickToActiveCaret: false);
        };

        icon.ContextMenuStrip = new ContextMenuStrip();
        icon.ContextMenuStrip.Items.Add("Open").Click += (sender, e) => navigator.OpenMain(stickToActiveCaret: false);
        icon.ContextMenuStrip.Items.Add("Configuration").Click += (sender, e) => navigator.OpenConfiguration();
        icon.ContextMenuStrip.Items.Add("About").Click += (sender, e) => navigator.OpenHelp();
        icon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); navigator.Shutdown(); };
    }

    public void Dispose()
    {
        if (icon != null)
            icon.Visible = false;
    }
}
