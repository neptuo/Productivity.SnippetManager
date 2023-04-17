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
    private readonly NotifyIcon icon;
    private readonly Hotkey hotkey;

    public TrayIcon(Navigator navigator, Hotkey hotkey)
    {
        this.hotkey = hotkey;

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
        BindHotkey(icon.ContextMenuStrip);
        icon.ContextMenuStrip.Items.Add("XML snippets").Click += (sender, e) => navigator.OpenXmlSnippets();
        icon.ContextMenuStrip.Items.Add("About").Click += (sender, e) => navigator.OpenHelp();
        icon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); navigator.Shutdown(); };
    }

    private void BindHotkey(ContextMenuStrip contextMenu)
    {
        bool isPaused = false;

        ToolStripItem menuItem = contextMenu.Items.Add("Pause hotkey");
        menuItem.Click += (sender, e) =>
        {
            if (isPaused)
            {
                menuItem.Text = "Pause hotkey";
                hotkey.Restore();
                isPaused = false;
            }
            else
            {
                menuItem.Text = "Restore hotkey";
                hotkey.Pause();
                isPaused = true;
            }
        };
    }

    public void Dispose()
    {
        if (icon != null)
            icon.Visible = false;
    }
}
