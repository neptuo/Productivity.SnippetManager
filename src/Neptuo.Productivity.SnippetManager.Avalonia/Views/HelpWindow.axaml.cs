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

    private void btnOpenGitHub_Click(object? sender, RoutedEventArgs e)
        => navigator?.OpenGitHub();

    private void btnOpenConfiguration_Click(object? sender, RoutedEventArgs e)
        => navigator?.OpenConfiguration();
}
