using System.Diagnostics;
using System.IO;

namespace Neptuo.Productivity.SnippetManager;

internal static class DiagnosticsLog
{
    private static readonly object SyncRoot = new();
    private static readonly string filePath = CreateFilePath();
    private static bool isInitialized;

    public static string FilePath => filePath;

    public static void Initialize(string[] args)
    {
        lock (SyncRoot)
        {
            EnsureLogDirectory();

            if (!isInitialized)
            {
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                Trace.Listeners.Add(new DiagnosticsTraceListener());
                isInitialized = true;
            }

            WriteUnlocked("INFO", $"Diagnostics logging initialized. File='{filePath}', pid={Environment.ProcessId}, args={FormatArgs(args)}");
        }
    }

    public static void Debug(string message)
        => Write("DEBUG", message);

    public static void Info(string message)
        => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
    {
        if (exception == null)
            Write("ERROR", message);
        else
            Write("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        => Error($"Unhandled exception (terminating={e.IsTerminating}).", e.ExceptionObject as Exception);

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        => Error("Unobserved task exception.", e.Exception);

    private static void Write(string level, string message)
    {
        lock (SyncRoot)
        {
            EnsureLogDirectory();
            WriteUnlocked(level, message);
        }
    }

    private static void WriteUnlocked(string level, string message)
    {
        string normalizedMessage = message.Replace(Environment.NewLine, Environment.NewLine + "    ");
        string line = $"{DateTimeOffset.Now:O} [{level}] {normalizedMessage}";

        try
        {
            File.AppendAllText(filePath, line + Environment.NewLine);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to append diagnostics log: {ex}");
        }
    }

    private static void EnsureLogDirectory()
    {
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);
    }

    private static string CreateFilePath()
    {
        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Logs",
                "Neptuo.Productivity.SnippetManager",
                "diagnostics.log");
        }

        string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrEmpty(root))
            root = Path.GetTempPath();

        return Path.Combine(root, "Neptuo.Productivity.SnippetManager", "diagnostics.log");
    }

    private static string FormatArgs(string[] args)
        => args.Length == 0 ? "<none>" : string.Join(", ", args);

    private sealed class DiagnosticsTraceListener : TraceListener
    {
        public override void Write(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                DiagnosticsLog.Write("TRACE", message);
        }

        public override void WriteLine(string? message)
        {
            if (!string.IsNullOrEmpty(message))
                DiagnosticsLog.Write("TRACE", message);
        }
    }
}
