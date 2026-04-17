using System.Diagnostics;
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
        BuildXmlSnippetsMenu(icon.ContextMenuStrip, navigator);
        icon.ContextMenuStrip.Items.Add("About").Click += (sender, e) => navigator.OpenHelp();
        icon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); navigator.Shutdown(); };
    }

    private static void BuildXmlSnippetsMenu(ContextMenuStrip contextMenu, Navigator navigator)
    {
        var xmlMenu = new ToolStripMenuItem("XML snippets");
        contextMenu.Items.Add(xmlMenu);

        contextMenu.Opening += (sender, e) =>
        {
            xmlMenu.DropDownItems.Clear();

            var filePaths = navigator.GetXmlSnippetFilePaths();
            if (filePaths.Count <= 1)
            {
                xmlMenu.DropDownItems.Add("Open").Click += (s, ev) => navigator.OpenXmlSnippets();
            }
            else
            {
                foreach (var path in filePaths)
                {
                    string label = Path.GetFileName(path);
                    var item = xmlMenu.DropDownItems.Add(label);
                    item.Click += (s, ev) => navigator.OpenXmlSnippets(path);
                }
            }
        };
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
