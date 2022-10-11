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

public class XmlSnippetProvider : ISnippetProvider, IDisposable
{
    private readonly List<SnippetModel> lastSnippets = new();
    private readonly List<SnippetModel> nextSnippets = new();
    private Task? loadSnippetsTask = null;
    private FileSystemWatcher? watcher;

    public Task InitializeAsync(SnippetProviderContext context)
    {
        string sourcePath = GetFilePath();
        if (!File.Exists(sourcePath))
            return Task.CompletedTask;

        watcher = new FileSystemWatcher(Path.GetDirectoryName(sourcePath)!, "*.xml")
        {
            //NotifyFilter = NotifyFilters.LastWrite
        };
        watcher.Changed += OnFileChanged;
        watcher.EnableRaisingEvents = true;

        return Task.Run(() =>
        {
            LoadSnippets(lastSnippets);
            context.Models.AddRange(lastSnippets);
        });
    }

    private static string GetFilePath()
        => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.xml");

    private static void LoadSnippets(ICollection<SnippetModel> result)
    {
        string sourcePath = GetFilePath();

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
            var model = new SnippetModel(title, text);

            result.Add(model);
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e) => loadSnippetsTask = Task.Run(() =>
    {
        lock (nextSnippets)
        {
            Thread.Sleep(100);
            nextSnippets.Clear();
            LoadSnippets(nextSnippets);
        }
    });

    public async Task UpdateAsync(SnippetProviderContext context)
    {
        if (loadSnippetsTask != null)
        {
            foreach (var snippet in lastSnippets)
                context.Models.Remove(snippet);

            await loadSnippetsTask;
            loadSnippetsTask = null;

            context.Models.AddRange(nextSnippets);
            lastSnippets.Clear();
            lastSnippets.AddRange(nextSnippets);
            nextSnippets.Clear();
        }
    }

    public void Dispose()
        => watcher?.Dispose();
}
