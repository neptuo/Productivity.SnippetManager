using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels;

namespace Neptuo.Productivity.SnippetManager.Views;

public partial class MainWindow : Window
{
    private PixelPoint? stickPoint;

    public MainViewModel ViewModel
    {
        get => (MainViewModel)DataContext!;
        set
        {
            if (DataContext is MainViewModel old)
                old.SearchCompleted -= SelectFirstSnippet;

            DataContext = value;

            if (value != null)
                value.SearchCompleted += SelectFirstSnippet;
        }
    }

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        UpdatePosition();
    }

    public void SetStickPoint(PixelPoint? point)
    {
        stickPoint = point;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel == null)
            return;

        if (ListBox.ItemCount > 0)
        {
            if (e.Key == Key.Down)
            {
                ListBox.SelectedIndex = (ListBox.SelectedIndex + 1) % ListBox.ItemCount;
                ListBox.ScrollIntoView(ListBox.SelectedIndex);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                int newIndex = ListBox.SelectedIndex - 1;
                if (newIndex < 0)
                    newIndex = ListBox.ItemCount - 1;

                ListBox.SelectedIndex = newIndex;
                ListBox.ScrollIntoView(ListBox.SelectedIndex);
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                ListBox.SelectedIndex = 0;
                ListBox.ScrollIntoView(ListBox.SelectedIndex);
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                ListBox.SelectedIndex = ListBox.ItemCount - 1;
                ListBox.ScrollIntoView(ListBox.SelectedIndex);
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
        else if (e.Key == Key.C && (e.KeyModifiers & KeyModifiers.Control) != 0)
        {
            UseSelectedSnippet(ViewModel.Copy);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            if (!string.IsNullOrEmpty(SearchText.Text))
                SearchText.Text = string.Empty;
            else if (ViewModel.UnSelectLast.CanExecute())
                ViewModel.UnSelectLast.Execute();
            else
                Close();

            e.Handled = true;
        }
        else if (e.Key == Key.Back)
        {
            if (string.IsNullOrEmpty(SearchText.Text) && ViewModel.UnSelectLast.CanExecute())
                ViewModel.UnSelectLast.Execute();
        }

        if (!e.Handled && !SearchText.IsFocused)
            SearchText.Focus();
    }

    private bool UseSelectedSnippet(Command<SnippetModel> command)
    {
        if (ListBox.SelectedItem is SnippetModel snippet && command.CanExecute(snippet))
        {
            command.Execute(snippet);
            return true;
        }

        return false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property.Name == nameof(IsActive) && change.NewValue is false)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (!IsActive)
                    Close();
            }, DispatcherPriority.Background);
        }
    }

    public void FocusSearchText()
        => SearchText.Focus();

    public void SelectFirstSnippet()
        => ListBox.SelectedIndex = 0;

    public void UpdatePosition()
    {
        if (stickPoint != null)
        {
            Position = stickPoint.Value;
        }
        else
        {
            var screen = Screens.Primary ?? Screens.All.FirstOrDefault();
            if (screen != null)
            {
                var x = (int)(screen.WorkingArea.X + (screen.WorkingArea.Width - Width) / 2);
                var y = (int)(screen.WorkingArea.Y + (screen.WorkingArea.Height - 300) / 2);
                Position = new PixelPoint(x, y);
            }
        }
    }

    public void Search()
        => ViewModel?.Search(SearchText.Text ?? string.Empty);

    private void SearchText_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.TextProperty)
            Search();
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SearchText.PropertyChanged += SearchText_PropertyChanged;
    }

    protected override void OnUnloaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        SearchText.PropertyChanged -= SearchText_PropertyChanged;
        base.OnUnloaded(e);
    }
}
