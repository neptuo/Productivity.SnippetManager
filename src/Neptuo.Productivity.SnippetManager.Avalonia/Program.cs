using Avalonia;

namespace Neptuo.Productivity.SnippetManager;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DiagnosticsLog.Initialize(args);
        DiagnosticsLog.Info("Starting Avalonia application.");

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            DiagnosticsLog.Info("Avalonia application exited normally.");
        }
        catch (Exception ex)
        {
            DiagnosticsLog.Error("Avalonia application terminated unexpectedly.", ex);
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions { ShowInDock = false })
            .LogToTrace();
}
