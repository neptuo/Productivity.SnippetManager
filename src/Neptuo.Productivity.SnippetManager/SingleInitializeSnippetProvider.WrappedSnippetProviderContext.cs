using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

partial class SingleInitializeSnippetProvider
{
    class WrappedSnippetProviderContext : SnippetProviderContext
    {
        public SnippetProviderContext Inner { get; set; }

        public WrappedSnippetProviderContext(SnippetProviderContext inner)
            => Inner = inner;

        public override void Add(SnippetModel snippet)
        {
            base.Add(snippet);
            Inner.Add(snippet);
        }

        public override void AddRange(IEnumerable<SnippetModel> snippets)
        {
            base.AddRange(snippets);
            Inner.AddRange(snippets);
        }

        public override void Remove(SnippetModel snippet)
        {
            base.Remove(snippet);
            Inner.Remove(snippet);
        }
    }
}
