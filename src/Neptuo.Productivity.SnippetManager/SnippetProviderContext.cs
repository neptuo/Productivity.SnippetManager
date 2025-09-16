using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace Neptuo.Productivity.SnippetManager
{
    [DebuggerDisplay("Entry {Model.Title}")]
    public record SnippetEntry(SnippetModel Model, string CurrentPath, SnippetModel? Parent)
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

        private static string PathSeparator = " - ";
        private static string[] GetPath(SnippetModel snippet) => snippet.Title.Split(PathSeparator);

        private void AddToTree(SnippetModel snippet)
        {
            string[] path = GetPath(snippet);

            SnippetModel? parent = null;
            List<SnippetEntry> children = root;
            for (var i = 0; i < path.Length - 1; i++)
            {
                var segment = path[i];

                var segmentEntry = children.Find(s => s.CurrentPath == segment);
                if (segmentEntry == null)
                {
                    var segmentModel = new SnippetModel(String.Join(PathSeparator, path[0..(i + 1)]));
                    children.Add(byModel[segmentModel] = segmentEntry = new(segmentModel, segment, parent));
                }

                parent = segmentEntry.Model;
                children = segmentEntry.Children;
            }

            string lastSegment = path[path.Length - 1];
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
            string[] path = GetPath(snippet);

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

                    var parentEntry = byModel[entry.Parent];
                    parentEntry.Children.Remove(entry);

                    if (parentEntry.Children.Count > 0 || !parentEntry.Model.IsShadow)
                        break;

                    entry = parentEntry;
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

        public IEnumerable<SnippetModel> GetChildren(SnippetModel parent)
        {
            if (byModel.TryGetValue(parent, out var entry))
                return entry.Children.Select(s => s.Model);

            return Enumerable.Empty<SnippetModel>();
        }

        public SnippetModel? FindParent(SnippetModel child)
        {
            if (byModel.TryGetValue(child, out var entry))
                return entry.Parent;

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
                    current = byModel[current.Parent];
                    while (lastAncestor != current.Model)
                    {
                        ancestors.Push(current.Model);
                        if (current.Parent == null)
                            break;

                        current = byModel[current.Parent];
                    }
                }
            }

            return ancestors;
        }
    }
}
