using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class SnippetProviderContext : ISnippetTree
    {
        private readonly List<SnippetModel> models = new();
        private readonly Dictionary<Guid, (SnippetModel model, List<SnippetModel>? children)> byId = new();

        public IEnumerable<SnippetModel> Models => models;

        public event Action? Changed;

        private void AddToTree(SnippetModel snippet)
        {
            byId[snippet.Id] = (snippet, null);

            if (snippet.ParentId != null)
            {
                var parent = byId[snippet.ParentId.Value];
                if (parent.children == null)
                    parent.children = new();

                parent.children.Add(snippet);
                byId[snippet.ParentId.Value] = parent;
            }
        }

        private void RemoveFromTree(SnippetModel snippet)
        {
            byId.Remove(snippet.Id);

            if (snippet.ParentId != null)
            {
                var parent = byId[snippet.ParentId.Value];
                parent.children!.Remove(snippet);
            }
        }

        public virtual void Add(SnippetModel snippet)
        {
            models.Add(snippet);
            AddToTree(snippet);
            Changed?.Invoke();
        }

        public virtual void AddRange(IEnumerable<SnippetModel> snippets)
        {
            models.AddRange(snippets);
            foreach (var snippet in snippets)
                AddToTree(snippet);

            Changed?.Invoke();
        }

        public virtual void Remove(SnippetModel snippet)
        {
            models.Remove(snippet);
            RemoveFromTree(snippet);
            Changed?.Invoke();
        }

        public IEnumerable<SnippetModel> GetRoots() 
            => models.Where(s => s.ParentId == null);

        public IEnumerable<SnippetModel> GetChildren(SnippetModel parent)
        {
            if (byId.TryGetValue(parent.Id, out var entry))
                return entry.children ?? Enumerable.Empty<SnippetModel>();

            return Enumerable.Empty<SnippetModel>();
        }
    }

    public interface ISnippetTree
    {
        IEnumerable<SnippetModel> GetRoots();
        IEnumerable<SnippetModel> GetChildren(SnippetModel parent);
    }
}
