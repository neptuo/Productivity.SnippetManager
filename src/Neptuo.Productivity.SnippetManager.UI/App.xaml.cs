using Neptuo.Windows.HotKeys;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Neptuo.Productivity.SnippetManager
{
    public partial class App : Application
    {
        private readonly Navigator navigator = new Navigator(new CompositeSnippetProvider(new DemoSnippetProvider(), new GuidSnippetProvider()));

        protected override void OnStartup(StartupEventArgs e)
        {
            var hotkeys = new ComponentDispatcherHotkeyCollection();
            hotkeys.Add(Key.V, ModifierKeys.Control | ModifierKeys.Shift, (_, _) =>
            {
                navigator.OpenMain();
            });
        }
    }
}
