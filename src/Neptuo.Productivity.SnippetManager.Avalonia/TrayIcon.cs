using System.IO;
using Avalonia;
using Avalonia.Controls;
using Neptuo.Productivity.SnippetManager.Plugins;
using NativeMenu = Avalonia.Controls.NativeMenu;
using NativeMenuItem = Avalonia.Controls.NativeMenuItem;
using TrayIconBase = Avalonia.Controls.TrayIcon;

namespace Neptuo.Productivity.SnippetManager;

public class TrayIcon : IDisposable
{
    private readonly TrayIconBase trayIcon;
    private readonly Hotkey hotkey;
    private readonly IReadOnlyList<ITrayMenuContributor> contributors;
    private readonly int contribInsertIndex;
    private int contribCount;
    private NativeMenuItem? hotkeyMenuItem;
    private bool isPaused;

    public TrayIcon(Navigator navigator, Hotkey hotkey, IEnumerable<ITrayMenuContributor> contributors)
    {
        this.hotkey = hotkey;
        this.contributors = contributors.ToArray();

        var menu = new NativeMenu();

        var openItem = new NativeMenuItem("Open");
        openItem.Click += (_, _) => navigator.Open(stickToActiveCaret: false);
        menu.Items.Add(openItem);

        var configItem = new NativeMenuItem("Configuration");
        configItem.Click += (_, _) => navigator.OpenConfiguration();
        menu.Items.Add(configItem);

        hotkeyMenuItem = new NativeMenuItem("Pause hotkey");
        hotkeyMenuItem.Click += (_, _) => ToggleHotkey();
        menu.Items.Add(hotkeyMenuItem);

        contribInsertIndex = menu.Items.Count;
        RebuildContributions(menu);

        var aboutItem = new NativeMenuItem("About");
        aboutItem.Click += (_, _) => navigator.OpenHelp();
        menu.Items.Add(aboutItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) =>
        {
            DiagnosticsLog.Info("Tray Exit menu item clicked.");
            navigator.CloseMain();
            navigator.Shutdown();
        };
        menu.Items.Add(exitItem);

        // Rebuild contributed items in place on every menu open so that
        // plugin state (e.g. the list of XML snippet files) is re-exported
        // to the macOS status bar menu. Mutating items via submenu.Menu = ...
        // is not re-exported by Avalonia on macOS once the menu has been
        // shown, so we replace the contributed NativeMenuItems entirely
        // inside the root menu's Items collection.
        menu.NeedsUpdate += (_, _) => RebuildContributions(menu);

        trayIcon = new TrayIconBase
        {
            ToolTipText = "Snippet Manager",
            Menu = menu,
            IsVisible = true
        };

        hotkey.HookFailed += OnHookFailed;

        // Set icon from embedded Avalonia resource
        try
        {
            string iconResource = OperatingSystem.IsMacOS()
                ? "avares://Neptuo.Productivity.SnippetManager.Avalonia/Resources/tray-icon.png"
                : "avares://Neptuo.Productivity.SnippetManager.Avalonia/Resources/icon.png";

            var uri = new Uri(iconResource);
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            trayIcon.Icon = new WindowIcon(stream);

            if (OperatingSystem.IsMacOS())
                MacOSProperties.SetIsTemplateIcon(trayIcon, true);
        }
        catch
        {
            // Icon not found, continue without it
        }

        trayIcon.Clicked += (_, _) => navigator.Open(stickToActiveCaret: false);
    }

    private void RebuildContributions(NativeMenu menu)
    {
        for (int i = 0; i < contribCount; i++)
            menu.Items.RemoveAt(contribInsertIndex);

        var builder = new Builder(menu, contribInsertIndex);
        foreach (var contributor in contributors)
            contributor.Contribute(builder);

        contribCount = builder.InsertedCount;
    }

    private sealed class Builder : ITrayMenuBuilder
    {
        private readonly NativeMenu menu;
        private readonly int insertIndex;
        public int InsertedCount { get; private set; }

        public Builder(NativeMenu menu, int insertIndex)
        {
            this.menu = menu;
            this.insertIndex = insertIndex;
        }

        public void AddItem(string label, Action onClick)
        {
            var item = new NativeMenuItem(label);
            item.Click += (_, _) => onClick();
            Insert(item);
        }

        public void AddSubMenu(string label, Action? onClick, Action<ITrayMenuBuilder> buildChildren)
        {
            var item = new NativeMenuItem(label);
            if (onClick != null)
                item.Click += (_, _) => onClick();

            var subMenu = new NativeMenu();
            var child = new SubBuilder(subMenu);
            buildChildren(child);
            if (child.InsertedCount > 0)
                item.Menu = subMenu;

            Insert(item);
        }

        public void AddSeparator()
            => Insert(new NativeMenuItemSeparator());

        private void Insert(NativeMenuItemBase item)
        {
            menu.Items.Insert(insertIndex + InsertedCount, item);
            InsertedCount++;
        }
    }

    private sealed class SubBuilder : ITrayMenuBuilder
    {
        private readonly NativeMenu menu;
        public int InsertedCount { get; private set; }

        public SubBuilder(NativeMenu menu)
        {
            this.menu = menu;
        }

        public void AddItem(string label, Action onClick)
        {
            var item = new NativeMenuItem(label);
            item.Click += (_, _) => onClick();
            menu.Items.Add(item);
            InsertedCount++;
        }

        public void AddSubMenu(string label, Action? onClick, Action<ITrayMenuBuilder> buildChildren)
        {
            var item = new NativeMenuItem(label);
            if (onClick != null)
                item.Click += (_, _) => onClick();

            var subMenu = new NativeMenu();
            var child = new SubBuilder(subMenu);
            buildChildren(child);
            if (child.InsertedCount > 0)
                item.Menu = subMenu;

            menu.Items.Add(item);
            InsertedCount++;
        }

        public void AddSeparator()
        {
            menu.Items.Add(new NativeMenuItemSeparator());
            InsertedCount++;
        }
    }

    private void OnHookFailed(string message)
    {
        trayIcon.ToolTipText = $"Snippet Manager — {message}";
        if (hotkeyMenuItem != null)
            hotkeyMenuItem.Header = "Hotkey unavailable";
    }

    private void ToggleHotkey()
    {
        if (isPaused)
        {
            hotkeyMenuItem!.Header = "Pause hotkey";
            hotkey.Restore();
            isPaused = false;
        }
        else
        {
            hotkeyMenuItem!.Header = "Restore hotkey";
            hotkey.Pause();
            isPaused = true;
        }
    }

    public void Dispose()
    {
        hotkey.HookFailed -= OnHookFailed;
        trayIcon.IsVisible = false;
        trayIcon.Dispose();
    }
}
