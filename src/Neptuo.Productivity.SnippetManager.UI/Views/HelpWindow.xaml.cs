using System.Windows;

namespace Neptuo.Productivity.SnippetManager.Views
{
    public partial class HelpWindow : Window
    {
        private Navigator navigator;

        public HelpWindow(Navigator navigator)
        {
            this.navigator = navigator;

            InitializeComponent();

            tblVersion.Text = ApplicationVersion.GetDisplayString();
        }

        private void btnOpenGitHub_Click(object sender, RoutedEventArgs e)
            => navigator.OpenGitHub();

        private void btnOpenConfiguration_Click(object sender, RoutedEventArgs e)
            => navigator.OpenConfiguration();
    }
}
