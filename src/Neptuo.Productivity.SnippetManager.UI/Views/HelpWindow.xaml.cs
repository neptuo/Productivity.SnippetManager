using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
