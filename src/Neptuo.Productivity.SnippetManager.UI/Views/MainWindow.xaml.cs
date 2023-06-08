using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using Neptuo.Productivity.SnippetManager.Views.Controls;
using Neptuo.Windows.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Neptuo.Productivity.SnippetManager.Views
{
    public partial class MainWindow : Window
    {
        private CaretPosition? stickPoint;

        public MainViewModel ViewModel
        {
            get => (MainViewModel)DataContext;
            set => DataContext = value;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            UpdatePosition();
        }

        public void SetStickPoint(CaretPosition? caret)
        {
            stickPoint = caret;
            isStickToBottom = false;
        }

        private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (WindowDrag.TryMove(e))
                DragMove();
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            FocusSearchText();
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (ViewModel == null)
                return;

            if (ListView.Items.Count > 0)
            {
                if (e.Key == Key.Down)
                {
                    ListView.SelectedIndex = (ListView.SelectedIndex + 1) % ListView.Items.Count;
                    ListView.ScrollIntoView(ListView.SelectedItem);
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    int newIndex = ListView.SelectedIndex - 1;
                    if (newIndex < 0)
                        newIndex = ListView.Items.Count - 1;

                    ListView.SelectedIndex = newIndex;
                    ListView.ScrollIntoView(ListView.SelectedItem);
                    e.Handled = true;
                }
                else if (e.Key == Key.PageUp)
                {
                    ListView.SelectedIndex = 0;
                    ListView.ScrollIntoView(ListView.SelectedItem);
                    e.Handled = true;
                }
                else if (e.Key == Key.PageDown)
                {
                    ListView.SelectedIndex = ListView.Items.Count - 1;
                    ListView.ScrollIntoView(ListView.SelectedItem);
                    e.Handled = true;
                }
            }

            if (e.Key == Key.Tab)
            {
                if (UseSelectedSnippet(ViewModel.Select))
                    SearchText.Text = string.Empty;

                e.Handled = true;
            }

            if (e.Key == Key.Enter)
            {
                UseSelectedSnippet(ViewModel.Apply);
                e.Handled = true;
            }
            else if (e.Key == Key.C && IsCtrlKeyPressed())
            {
                UseSelectedSnippet(ViewModel.Copy);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (!String.IsNullOrEmpty(SearchText.Text))
                    SearchText.Text = string.Empty;
                else if (ViewModel.UnSelectLast.CanExecute())
                    ViewModel.UnSelectLast.Execute();
                else
                    Close();

                e.Handled = true;
            }
            else if (e.Key == Key.Back)
            {
                if (String.IsNullOrEmpty(SearchText.Text) && ViewModel.UnSelectLast.CanExecute())
                    ViewModel.UnSelectLast.Execute();
            }

            // Lastly, if non of the hot keys was pressed. Try to focus search box.
            if (!e.Handled && !SearchText.IsFocused)
                SearchText.Focus();
        }

        private bool IsCtrlKeyPressed()
            => Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

        private void ListView_Click(object sender, MouseButtonEventArgs e)
            => UseSelectedSnippet(ViewModel.Apply);

        private bool UseSelectedSnippet(Command<SnippetModel> command)
        {
            if (ListView.SelectedItem is SnippetModel snippet && command.CanExecute(snippet))
            {
                command.Execute(snippet);
                return true;
            }

            return false;
        }

        private void SearchText_TextChanged(object sender, TextChangedEventArgs e)
            => Search();

        public void Search()
        {
            ViewModel.Search(SearchText.Text);
            SelectFirstSnippet();
        }

        private void ListView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((ListView.SelectedIndex == 0 && e.Key == Key.Up) || e.Key == Key.Escape)
            {
                ListView.SelectedIndex = -1;
                SearchText.Focus();
                e.Handled = true;
            }
        }

        public void FocusSearchText()
            => SearchText.Focus();

        public void SelectFirstSnippet()
            => ListView.SelectedIndex = 0;

        private void OnDeactivated(object sender, EventArgs e)
        {
            DispatcherHelper.Run(Dispatcher, () =>
            {
                if (!IsActive)
                    Close();
            }, 500);
        }

        private bool isStickToBottom;

        public void UpdatePosition()
        {
            isStickToBottom = false;
            if (stickPoint == null)
            {
                Screen screen = GetTargetScreen();
                Left = screen.WorkingArea.Left + (screen.WorkingArea.Width - ActualWidth) / 2;
                Top = screen.WorkingArea.Top + (screen.WorkingArea.Height - ActualHeight) / 2;
            }
            else
            {
                StickToCaret();
            }
        }

        private Screen GetTargetScreen()
            => Screen.FromHandle(stickPoint?.WindowHandle ?? Win32.GetForegroundWindow());

        private void StickToCaret()
        {
            if (stickPoint == null)
                return;

            Screen screen = GetTargetScreen();

            var x = stickPoint.Position.Right;
            if (x + ActualWidth <= screen.WorkingArea.X + screen.WorkingArea.Width)
                Left = x;
            else
                Left = x - ActualWidth;

            if (!isStickToBottom && stickPoint.Position.Bottom + ActualHeight <= screen.WorkingArea.Y + screen.WorkingArea.Height)
            {
                Top = stickPoint.Position.Bottom;
            }
            else
            {
                Top = stickPoint.Position.Top - ActualHeight;
                isStickToBottom = true;
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            StickToCaret();
        }

        class Win32
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
        }
    }
}
