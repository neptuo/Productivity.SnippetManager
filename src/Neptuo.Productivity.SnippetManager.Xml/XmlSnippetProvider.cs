using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

public class XmlSnippetProvider : SingleInitializeSnippetProvider, IDisposable
{
    private readonly List<SnippetModel> lastSnippets = new();
    private readonly List<SnippetModel> nextSnippets = new();
    private readonly XmlConfiguration configuration;
    private Task? loadSnippetsTask = null;
    private FileSystemWatcher? watcher;

    public XmlSnippetProvider(XmlConfiguration configuration)
        => this.configuration = configuration;

    protected override Task InitializeOnceAsync(SnippetProviderContext context)
    {
        string sourcePath = configuration.GetFilePathOrDefault();
        if (!File.Exists(sourcePath))
            return Task.CompletedTask;

        watcher = new FileSystemWatcher(Path.GetDirectoryName(sourcePath)!, "*.xml");
        watcher.Changed += OnFileChanged;
        watcher.EnableRaisingEvents = true;

        return Task.Run(() =>
        {
            LoadSnippets(lastSnippets);
            context.AddRange(lastSnippets);
        });
    }

    private void LoadSnippets(ICollection<SnippetModel> result)
    {
        string sourcePath = configuration.GetFilePathOrDefault();

        XmlSnippetRoot? root;
        XmlSerializer serializer = new XmlSerializer(typeof(XmlSnippetRoot));
        using (FileStream sourceContent = new FileStream(sourcePath, FileMode.Open))
            root = (XmlSnippetRoot?)serializer.Deserialize(sourceContent);

        if (root == null || root.Snippets == null)
            return;

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

    private int MapPriority(XmlSnippetPriority priority) => priority switch
    {
        XmlSnippetPriority.Most => SnippetPriority.Most,
        XmlSnippetPriority.High => SnippetPriority.High,
        XmlSnippetPriority.Normal => SnippetPriority.Normal,
        XmlSnippetPriority.Low => SnippetPriority.Low,
        _ => throw Ensure.Exception.NotSupported(priority)
    };

    private void OnFileChanged(object sender, FileSystemEventArgs e) => loadSnippetsTask = Task.Run(() =>
    {
        if (e.FullPath == configuration.GetFilePathOrDefault())
        {
            lock (nextSnippets)
            {
                Thread.Sleep(100);
                nextSnippets.Clear();
                LoadSnippets(nextSnippets);
            }
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

            context.AddRange(nextSnippets);
            lastSnippets.Clear();
            lastSnippets.AddRange(nextSnippets);
            nextSnippets.Clear();
        }
    }

    public void Dispose()
        => watcher?.Dispose();
}
