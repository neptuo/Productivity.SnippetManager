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

        var xmlItem = new NativeMenuItem("XML snippets");
        xmlItem.Click += (_, _) => navigator.OpenXmlSnippets();
        menu.Items.Add(xmlItem);

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

        trayIcon = new TrayIconBase
        {
            ToolTipText = "Snippet Manager",
            Menu = menu,
            IsVisible = true
        };

        // Set icon from embedded Avalonia resource
        try
        {
            var uri = new Uri("avares://Neptuo.Productivity.SnippetManager.Avalonia/Resources/icon.png");
            using var stream = Avalonia.Platform.AssetLoader.Open(uri);
            trayIcon.Icon = new WindowIcon(stream);
        }
        catch
        {
            // Icon not found, continue without it
        }

        trayIcon.Clicked += (_, _) => navigator.OpenMain(stickToActiveCaret: false);
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
        trayIcon.IsVisible = false;
        trayIcon.Dispose();
    }
}
