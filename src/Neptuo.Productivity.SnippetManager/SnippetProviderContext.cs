using Neptuo.Collections.Generic;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class SnippetProviderContext
    {
        private ICollection<SnippetModel> models;

        public IEnumerable<SnippetModel> Models => models;

        public event Action? Changed;

        public SnippetProviderContext(ICollection<SnippetModel> models) 
            => this.models = models;

        public virtual void Add(SnippetModel snippet)
        {
            models.Add(snippet);
            Changed?.Invoke();
        }

        public virtual void AddRange(IEnumerable<SnippetModel> snippets)
        {
            models.AddRange(snippets);
            Changed?.Invoke();
        }

        public virtual void Remove(SnippetModel snippet)
        {
            models.Remove(snippet);
            Changed?.Invoke();
        }
    }
}
