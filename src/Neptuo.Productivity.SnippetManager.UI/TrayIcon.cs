using System.Diagnostics;
using System.Windows.Forms;
using Neptuo.Productivity.SnippetManager.Plugins;

namespace Neptuo.Productivity.SnippetManager;

public class TrayIcon : IDisposable
{
    private readonly NotifyIcon icon;
    private readonly Hotkey hotkey;

    public TrayIcon(Navigator navigator, Hotkey hotkey, IEnumerable<ITrayMenuContributor> contributors)
    {
        this.hotkey = hotkey;
        var contributorList = contributors.ToArray();

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

        var contextMenu = new ContextMenuStrip();
        icon.ContextMenuStrip = contextMenu;

        contextMenu.Items.Add("Open").Click += (sender, e) => navigator.OpenMain(stickToActiveCaret: false);
        contextMenu.Items.Add("Configuration").Click += (sender, e) => navigator.OpenConfiguration();
        BindHotkey(contextMenu);

        int contribInsertIndex = contextMenu.Items.Count;
        int contribCount = 0;

        contextMenu.Items.Add("About").Click += (sender, e) => navigator.OpenHelp();
        contextMenu.Items.Add("Exit").Click += (sender, e) => { navigator.CloseMain(); navigator.Shutdown(); };

        // Rebuild contributed items on every menu open so plugin state
        // (e.g. the list of XML snippet files) stays fresh without restart.
        void RebuildContributions()
        {
            for (int i = 0; i < contribCount; i++)
            {
                ToolStripItem item = contextMenu.Items[contribInsertIndex];
                contextMenu.Items.RemoveAt(contribInsertIndex);
                item.Dispose();
            }

            var builder = new Builder(contextMenu.Items, contribInsertIndex);
            foreach (var contributor in contributorList)
                contributor.Contribute(builder);

            contribCount = builder.InsertedCount;
        }

        RebuildContributions();
        contextMenu.Opening += (s, e) => RebuildContributions();
    }

    private sealed class Builder : ITrayMenuBuilder
    {
        private readonly ToolStripItemCollection items;
        private readonly int insertIndex;
        public int InsertedCount { get; private set; }

        public Builder(ToolStripItemCollection items, int insertIndex)
        {
            this.items = items;
            this.insertIndex = insertIndex;
        }

        public void AddItem(string label, Action onClick)
        {
            var item = new ToolStripMenuItem(label);
            item.Click += (_, _) => onClick();
            Insert(item);
        }

        public void AddSubMenu(string label, Action? onClick, Action<ITrayMenuBuilder> buildChildren)
        {
            var item = new ToolStripMenuItem(label);
            if (onClick != null)
                item.Click += (_, _) => onClick();

            var child = new SubBuilder(item.DropDownItems);
            buildChildren(child);

            Insert(item);
        }

        public void AddSeparator()
            => Insert(new ToolStripSeparator());

        private void Insert(ToolStripItem item)
        {
            items.Insert(insertIndex + InsertedCount, item);
            InsertedCount++;
        }
    }

    private sealed class SubBuilder : ITrayMenuBuilder
    {
        private readonly ToolStripItemCollection items;

        public SubBuilder(ToolStripItemCollection items)
        {
            this.items = items;
        }

        public void AddItem(string label, Action onClick)
        {
            var item = new ToolStripMenuItem(label);
            item.Click += (_, _) => onClick();
            items.Add(item);
        }

        public void AddSubMenu(string label, Action? onClick, Action<ITrayMenuBuilder> buildChildren)
        {
            var item = new ToolStripMenuItem(label);
            if (onClick != null)
                item.Click += (_, _) => onClick();

            var child = new SubBuilder(item.DropDownItems);
            buildChildren(child);
            items.Add(item);
        }

        public void AddSeparator()
            => items.Add(new ToolStripSeparator());
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
