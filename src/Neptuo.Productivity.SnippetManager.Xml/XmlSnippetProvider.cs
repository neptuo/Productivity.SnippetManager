using Neptuo.Productivity.SnippetManager.Models;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

public class XmlSnippetProvider(XmlConfiguration configuration) : SingleInitializeSnippetProvider, IDisposable
{
    private readonly List<SnippetModel> lastSnippets = new();
    private readonly List<SnippetModel> nextSnippets = new();
    private Task? loadSnippetsTask = null;
    private readonly List<FileSystemWatcher> watchers = new();
    private List<string> resolvedFilePaths = new();

    /// <summary>
    /// Returns the list of all XML files involved (root + includes), resolved after initialization or reload.
    /// </summary>
    public IReadOnlyList<string> ResolvedFilePaths => resolvedFilePaths;

    protected override Task InitializeOnceAsync(SnippetProviderContext context)
    {
        string sourcePath = configuration.GetFilePathOrDefault();
        Trace.WriteLine($"XmlSnippetProvider.InitializeOnceAsync sourcePath='{sourcePath}' exists={File.Exists(sourcePath)}");
        if (!File.Exists(sourcePath))
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            var filePaths = new List<string>();
            LoadSnippets(lastSnippets, sourcePath, filePaths);
            resolvedFilePaths = filePaths;
            Trace.WriteLine($"XmlSnippetProvider.InitializeOnceAsync resolved {filePaths.Count} file(s): [{string.Join(", ", filePaths)}]");
            SetupWatchers(filePaths);
            context.AddRange(lastSnippets);
        });
    }

    private void LoadSnippets(ICollection<SnippetModel> result, string rootPath, List<string> allFilePaths)
    {
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        LoadSnippetsRecursive(result, rootPath, visited, allFilePaths);
    }

    private void LoadSnippetsRecursive(ICollection<SnippetModel> result, string filePath, HashSet<string> visited, List<string> allFilePaths)
    {
        string absolutePath = Path.GetFullPath(filePath);
        if (!visited.Add(absolutePath))
            return;

        if (!File.Exists(absolutePath))
            return;

        allFilePaths.Add(absolutePath);

        XmlSnippetRoot? root;
        XmlSerializer serializer = new XmlSerializer(typeof(XmlSnippetRoot));
        using (FileStream sourceContent = new FileStream(absolutePath, FileMode.Open))
            root = (XmlSnippetRoot?)serializer.Deserialize(sourceContent);

        if (root == null)
            return;

        if (root.Snippets != null)
        {
            foreach (var snippet in root.Snippets)
            {
                string? text = snippet.TextAttribute ?? snippet.Text;
                if (text == null)
                    continue;

                string title = snippet.Title ?? text;
                var model = new SnippetModel(title, text, priority: MapPriority(snippet.Priority));

                result.Add(model);
            }
        }

        if (root.Includes != null)
        {
            string? baseDir = Path.GetDirectoryName(absolutePath);
            foreach (var include in root.Includes)
            {
                if (string.IsNullOrEmpty(include.Path))
                    continue;

                string includePath = baseDir != null
                    ? Path.GetFullPath(include.Path, baseDir)
                    : Path.GetFullPath(include.Path);

                LoadSnippetsRecursive(result, includePath, visited, allFilePaths);
            }
        }
    }

    private int MapPriority(XmlSnippetPriority priority) => priority switch
    {
        XmlSnippetPriority.Most => SnippetPriority.Most,
        XmlSnippetPriority.High => SnippetPriority.High,
        XmlSnippetPriority.Normal => SnippetPriority.Normal,
        XmlSnippetPriority.Low => SnippetPriority.Low,
        _ => throw Ensure.Exception.NotSupported(priority)
    };

    private void SetupWatchers(List<string> filePaths)
    {
        DisposeWatchers();

        var directories = filePaths
            .Select(p => Path.GetDirectoryName(p)!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Trace.WriteLine($"XmlSnippetProvider.SetupWatchers watching {directories.Count} directory(ies): [{string.Join(", ", directories)}]");

        foreach (var dir in directories)
        {
            if (dir != null && Directory.Exists(dir))
            {
                var watcher = new FileSystemWatcher(dir, "*.xml");
                watcher.Changed += OnFileChanged;
                watcher.Created += OnFileChanged;
                watcher.Renamed += OnFileChanged;
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
            else
            {
                Trace.WriteLine($"XmlSnippetProvider.SetupWatchers skipped missing directory '{dir}'");
            }
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e) => loadSnippetsTask = Task.Run(() =>
    {
        Trace.WriteLine($"XmlSnippetProvider.OnFileChanged {e.ChangeType} '{e.FullPath}'");
        if (resolvedFilePaths.Contains(e.FullPath, StringComparer.OrdinalIgnoreCase))
        {
            lock (nextSnippets)
            {
                Thread.Sleep(100);
                nextSnippets.Clear();
                var filePaths = new List<string>();
                string sourcePath = configuration.GetFilePathOrDefault();
                LoadSnippets(nextSnippets, sourcePath, filePaths);
                resolvedFilePaths = filePaths;
                Trace.WriteLine($"XmlSnippetProvider.OnFileChanged reloaded, now {filePaths.Count} file(s): [{string.Join(", ", filePaths)}]");
            }
        }
        else
        {
            Trace.WriteLine($"XmlSnippetProvider.OnFileChanged ignored (not in resolvedFilePaths)");
        }
    });

    protected override async Task UpdateOverrideAsync(SnippetProviderContext context)
    {
        if (loadSnippetsTask != null)
        {
            foreach (var snippet in lastSnippets)
                context.Remove(snippet);

            await loadSnippetsTask;
            loadSnippetsTask = null;

            SetupWatchers(resolvedFilePaths);
            context.AddRange(nextSnippets);
            lastSnippets.Clear();
            lastSnippets.AddRange(nextSnippets);
            nextSnippets.Clear();
        }
    }

    private void DisposeWatchers()
    {
        foreach (var watcher in watchers)
            watcher.Dispose();

        watchers.Clear();
    }

    public void Dispose()
        => DisposeWatchers();
}
