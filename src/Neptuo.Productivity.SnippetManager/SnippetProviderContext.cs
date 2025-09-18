using Neptuo.Productivity.SnippetManager.Models;
using System.Diagnostics;

namespace Neptuo.Productivity.SnippetManager
{
    [DebuggerDisplay("Entry {Model.Title}")]
    public record SnippetEntry(SnippetModel Model, string CurrentPath, SnippetEntry? Parent)
    {
        public SnippetModel Model { get; set; } = Model;
        public List<SnippetEntry> Children { get; } = new();
    }

    public class SnippetProviderContext : ISnippetTree
    {
        private readonly List<SnippetModel> models = new();
        private readonly List<SnippetEntry> root = new();
        private readonly Dictionary<SnippetModel, SnippetEntry> byModel = new();

        public IEnumerable<SnippetModel> Models => models;

        public event Action? Changed;

        private void AddToTree(SnippetModel snippet)
        {
            SnippetEntry? parent = null;
            List<SnippetEntry> children = root;
            for (var i = 0; i < snippet.Path.Length - 1; i++)
            {
                var segment = snippet.Path[i];

                var segmentEntry = children.Find(s => s.CurrentPath == segment);
                if (segmentEntry == null)
                {
                    var segmentModel = new SnippetModel(String.Join(SnippetModel.PathSeparator, snippet.Path[0..(i + 1)]));
                    children.Add(byModel[segmentModel] = segmentEntry = new(segmentModel, segment, parent));
                }

                parent = segmentEntry;
                children = segmentEntry.Children;
            }

            string lastSegment = snippet.Path[snippet.Path.Length - 1];
            var snippetEntry = children.Find(s => s.CurrentPath == lastSegment);
            if (snippetEntry == null)
            {
                children.Add(snippetEntry = new(snippet, lastSegment, parent));
            }
            else
            {
                byModel.Remove(snippetEntry.Model);
                models.Remove(snippetEntry.Model);
                snippetEntry.Model = snippet;
            }

            byModel[snippet] = snippetEntry;
            models.Add(snippet);
        }

        private void RemoveFromTree(SnippetModel snippet)
        {
            if (byModel.TryGetValue(snippet, out var entry))
            {
                void RemoveSnippetEntry(SnippetEntry e)
                {
                    byModel.Remove(e.Model);
                    models.Remove(e.Model);

                    foreach (var child in e.Children)
                        RemoveSnippetEntry(child);
                }

                RemoveSnippetEntry(entry);

                while (entry.Parent != null)
                {
                    byModel.Remove(entry.Model);

                    entry.Parent.Children.Remove(entry);
                    if (entry.Parent.Children.Count > 0 || !entry.Parent.Model.IsShadow)
                        break;

                    entry = entry.Parent;
                }
            }
            else
            {
                throw Ensure.Exception.InvalidOperation($"Snippet '{snippet.Title}' was not found in the tree");
            }
        }

        public virtual void Add(SnippetModel snippet)
        {
            AddToTree(snippet);
            Changed?.Invoke();
        }

        public virtual void AddRange(IEnumerable<SnippetModel> snippets)
        {
            foreach (var snippet in snippets)
                AddToTree(snippet);

            Changed?.Invoke();
        }

        public virtual void Remove(SnippetModel snippet)
        {
            RemoveFromTree(snippet);
            Changed?.Invoke();
        }

        public IEnumerable<SnippetModel> GetRoots() 
            => root.Select(s => s.Model);

        public bool HasChildren(SnippetModel parent)
        {
            if (byModel.TryGetValue(parent, out var entry))
                return entry.Children.Count > 0;

            return false;
        }

        public IEnumerable<SnippetModel> GetChildren(SnippetModel parent)
        {
            if (byModel.TryGetValue(parent, out var entry))
                return entry.Children.Select(s => s.Model);

            return Enumerable.Empty<SnippetModel>();
        }

        public SnippetModel? FindParent(SnippetModel child)
        {
            if (byModel.TryGetValue(child, out var entry))
                return entry.Parent?.Model;

            return null;
        }

        public IEnumerable<SnippetModel> GetAncestors(SnippetModel child, SnippetModel? lastAncestor = null)
        {
            var ancestors = new Stack<SnippetModel>();
            if (byModel.TryGetValue(child, out var entry))
            {
                SnippetEntry? current = entry;
                if (current?.Parent != null)
                {
                    // We need to navigate to parent of the 'child'.
                    // The 'child' should never be added.
                    current = current.Parent;
                    while (lastAncestor != current.Model)
                    {
                        ancestors.Push(current.Model);
                        if (current.Parent == null)
                            break;

                        current = current.Parent;
                    }
                }
            }

            return ancestors;
        }
    }
}
