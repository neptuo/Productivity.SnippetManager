using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Neptuo.Productivity.SnippetManager.Views;

public partial class HelpWindow : Window
{
    private Navigator? navigator;

    public HelpWindow()
    {
        InitializeComponent();
    }

    public HelpWindow(Navigator navigator) : this()
    {
        this.navigator = navigator;
        tblVersion.Text = ApplicationVersion.GetDisplayString();
    }

    public void SetHotkey(string hotkey)
        => tblHotkey.Text = $"Global hotkey: {hotkey}";

    private void btnOpenGitHub_Click(object? sender, RoutedEventArgs e)
        => navigator?.OpenGitHub();

    private void btnOpenConfiguration_Click(object? sender, RoutedEventArgs e)
        => navigator?.OpenConfiguration();

    private void btnOpenLogs_Click(object? sender, RoutedEventArgs e)
        => navigator?.OpenLogs();
}
