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
    private WindowPositionAnchor? positionAnchor;
    private bool isAnchorPositionUpdateQueued;

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
        DiagnosticsLog.Info($"Main window opened. position={FormatPixelPoint(Position)}, anchor={FormatAnchor(positionAnchor)}, width={Width}, height={Height}, bounds={Bounds}, clientSize={ClientSize}.");
        UpdatePosition();
    }

    internal void SetPositionAnchor(WindowPositionAnchor? anchor)
    {
        positionAnchor = anchor;
        DiagnosticsLog.Info(anchor == null
            ? "Main window anchor cleared. Positioning will use screen centering."
            : $"Main window anchor set to {FormatAnchor(anchor)}.");
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

        if (change.Property == BoundsProperty && positionAnchor != null && IsVisible && change.NewValue is Rect bounds && bounds.Width > 0 && bounds.Height > 0)
            QueueAnchorPositionUpdate($"window bounds changed to {bounds}");

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
        var screen = GetTargetScreen();
        double positioningWidth = GetPositioningDimension(ClientSize.Width, Bounds.Width, Width, WindowPositioning.DefaultWidth);
        double positioningHeight = GetPositioningDimension(ClientSize.Height, Bounds.Height, Height, WindowPositioning.DefaultHeight);

        DiagnosticsLog.Info($"Updating main window position. currentPosition={FormatPixelPoint(Position)}, anchor={FormatAnchor(positionAnchor)}, positioningWidth={positioningWidth}, positioningHeight={positioningHeight}, width={Width}, height={Height}, bounds={Bounds}, clientSize={ClientSize}.");

        if (screen == null)
        {
            DiagnosticsLog.Info("Unable to determine a target screen for main window positioning.");
            return;
        }

        Position = WindowPositioning.CalculatePosition(positionAnchor, screen.WorkingArea, positioningWidth, positioningHeight);

        if (positionAnchor is { } anchor)
        {
            DiagnosticsLog.Info($"Positioned main window near the {anchor.Source} anchor {FormatPixelRect(anchor.Bounds)} on screen bounds={screen.Bounds}, workingArea={screen.WorkingArea}, targetPosition={FormatPixelPoint(Position)}.");
        }
        else
        {
            DiagnosticsLog.Info($"Centered main window on screen bounds={screen.Bounds}, workingArea={screen.WorkingArea}, targetPosition={FormatPixelPoint(Position)}.");
        }

        Dispatcher.UIThread.Post(() =>
        {
            DiagnosticsLog.Info($"Main window position update completed. finalPosition={FormatPixelPoint(Position)}, bounds={Bounds}, clientSize={ClientSize}.");
        }, DispatcherPriority.Background);
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

    private void QueueAnchorPositionUpdate(string reason)
    {
        if (isAnchorPositionUpdateQueued)
            return;

        isAnchorPositionUpdateQueued = true;
        DiagnosticsLog.Info($"Scheduling an anchor-based position update because {reason}.");
        Dispatcher.UIThread.Post(() =>
        {
            isAnchorPositionUpdateQueued = false;
            if (positionAnchor != null && IsVisible)
                UpdatePosition();
        }, DispatcherPriority.Background);
    }

    private global::Avalonia.Platform.Screen? GetTargetScreen()
    {
        if (positionAnchor is { } anchor)
        {
            int centerX = anchor.Bounds.X + Math.Max(0, anchor.Bounds.Width / 2);
            int centerY = anchor.Bounds.Y + Math.Max(0, anchor.Bounds.Height / 2);

            global::Avalonia.Platform.Screen? anchorScreen = Screens.All.FirstOrDefault(s => Contains(s.Bounds, centerX, centerY))
                ?? Screens.All.FirstOrDefault(s => Intersects(s.Bounds, anchor.Bounds));
            if (anchorScreen != null)
                return anchorScreen;
        }

        return Screens.Primary ?? Screens.All.FirstOrDefault();
    }

    private static bool Contains(PixelRect rect, int x, int y)
        => x >= rect.X && x < rect.X + rect.Width && y >= rect.Y && y < rect.Y + rect.Height;

    private static bool Intersects(PixelRect left, PixelRect right)
        => left.X < right.X + right.Width
        && left.X + left.Width > right.X
        && left.Y < right.Y + right.Height
        && left.Y + left.Height > right.Y;

    private static double GetPositioningDimension(double preferred, double secondary, double tertiary, int defaultValue)
    {
        if (IsUsableDimension(preferred))
            return preferred;

        if (IsUsableDimension(secondary))
            return secondary;

        if (IsUsableDimension(tertiary))
            return tertiary;

        return defaultValue;
    }

    private static bool IsUsableDimension(double value)
        => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;

    private static string FormatPixelPoint(PixelPoint? point)
        => point.HasValue ? $"({point.Value.X}, {point.Value.Y})" : "<none>";

    private static string FormatPixelRect(PixelRect rect)
        => $"({rect.X}, {rect.Y}, {rect.Width}, {rect.Height})";

    private static string FormatAnchor(WindowPositionAnchor? anchor)
        => anchor is { } value
            ? $"{value.Source} {FormatPixelRect(value.Bounds)}"
            : "<none>";
}
