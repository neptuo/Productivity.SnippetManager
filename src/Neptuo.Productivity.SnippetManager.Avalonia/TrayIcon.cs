using System.IO;
using Avalonia;
using Avalonia.Controls;
using NativeMenu = Avalonia.Controls.NativeMenu;
using NativeMenuItem = Avalonia.Controls.NativeMenuItem;
using TrayIconBase = Avalonia.Controls.TrayIcon;

namespace Neptuo.Productivity.SnippetManager;

public class TrayIcon : IDisposable
{
    private readonly TrayIconBase trayIcon;
    private readonly Hotkey hotkey;
    private NativeMenuItem? hotkeyMenuItem;
    private bool isPaused;

    public TrayIcon(Navigator navigator, Hotkey hotkey)
    {
        this.hotkey = hotkey;

        var menu = new NativeMenu();

        var openItem = new NativeMenuItem("Open");
        openItem.Click += (_, _) => navigator.OpenMain(stickToActiveCaret: false);
        menu.Items.Add(openItem);

        var configItem = new NativeMenuItem("Configuration");
        configItem.Click += (_, _) => navigator.OpenConfiguration();
        menu.Items.Add(configItem);

        hotkeyMenuItem = new NativeMenuItem("Pause hotkey");
        hotkeyMenuItem.Click += (_, _) => ToggleHotkey();
        menu.Items.Add(hotkeyMenuItem);

        int xmlIndex = menu.Items.Count;
        menu.Items.Add(BuildXmlSnippetsMenuItem(navigator));

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

        // Rebuild the XML snippets item in place on every menu open so that
        // newly included files (or a single-file fallback) are re-exported to
        // the macOS status bar menu. Mutating a submenu via xmlMenu.Menu=...
        // is not re-exported by Avalonia on macOS once the item has been
        // shown, so we replace the NativeMenuItem entirely inside the root
        // menu's Items collection.
        menu.NeedsUpdate += (_, _) => menu.Items[xmlIndex] = BuildXmlSnippetsMenuItem(navigator);

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

        trayIcon.Clicked += (_, _) => navigator.OpenMain(stickToActiveCaret: false);
    }

    private static NativeMenuItem BuildXmlSnippetsMenuItem(Navigator navigator)
    {
        var xmlMenu = new NativeMenuItem("XML snippets");
        var filePaths = navigator.GetXmlSnippetFilePaths();

        if (filePaths.Count > 1)
        {
            var subMenu = new NativeMenu();
            foreach (var path in filePaths)
            {
                string label = Path.GetFileName(path);
                var item = new NativeMenuItem(label);
                var capturedPath = path;
                item.Click += (_, _) => navigator.OpenXmlSnippets(capturedPath);
                subMenu.Items.Add(item);
            }
            xmlMenu.Menu = subMenu;
        }
        else
        {
            xmlMenu.Click += (_, _) => navigator.OpenXmlSnippets();
        }

        return xmlMenu;
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
