using System.Diagnostics;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public class TrayIcon : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly Hotkey hotkey;

    public TrayIcon(Navigator navigator, Hotkey hotkey, Action<Action> subscribeXmlFilesChanged)
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
        BuildXmlSnippetsMenu(icon.ContextMenuStrip, navigator, subscribeXmlFilesChanged);
        icon.ContextMenuStrip.Items.Add("About").Click += (sender, e) => navigator.OpenHelp();
        icon.ContextMenuStrip.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); navigator.Shutdown(); };
    }

    private static void BuildXmlSnippetsMenu(ContextMenuStrip contextMenu, Navigator navigator, Action<Action> subscribeXmlFilesChanged)
    {
        var xmlMenu = new ToolStripMenuItem("XML snippets");
        xmlMenu.Click += (s, ev) => navigator.OpenXmlSnippets();
        contextMenu.Items.Add(xmlMenu);

        void Rebuild()
        {
            xmlMenu.DropDownItems.Clear();

            var filePaths = navigator.GetXmlSnippetFilePaths();
            if (filePaths.Count > 1)
            {
                foreach (var path in filePaths)
                {
                    string label = Path.GetFileName(path);
                    var item = xmlMenu.DropDownItems.Add(label);
                    item.Click += (s, ev) => navigator.OpenXmlSnippets(path);
                }
            }
        }

        Rebuild();
        subscribeXmlFilesChanged(Rebuild);
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
