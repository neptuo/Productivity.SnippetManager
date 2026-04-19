using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;

namespace Neptuo.Productivity.SnippetManager.ViewModels
{
    public class MainViewModel : ObservableModel
    {
        public ObservableCollection<SnippetModel> Selected { get; } = new ObservableCollection<SnippetModel>();
        public ObservableCollection<SnippetModel> Snippets { get; } = new ObservableCollection<SnippetModel>();

        public ApplySnippetCommand Apply { get; }
        public CopySnippetCommand Copy { get; }
        public DelegateCommand<SnippetModel> Select { get; }
        public DelegateCommand UnSelectLast { get; }

        private ISnippetTree snippetTree;
        private SnippetSearcher searcher;

        private bool isInitializing;
        public bool IsInitializing
        {
            get { return isInitializing; }
            set
            {
                if (isInitializing != value)
                {
                    isInitializing = value;
                    RaisePropertyChanged();
                }
            }
        }

        public event Action SearchCompleted = () => { };

        public MainViewModel(ISnippetTree snippetTree, ApplySnippetCommand apply, CopySnippetCommand copy)
        {
            this.snippetTree = snippetTree;
            this.searcher = new(snippetTree, 5);
            Apply = apply;
            Copy = copy;
            Select = new DelegateCommand<SnippetModel>(SelectExecute, CanSelectExecute);
            UnSelectLast = new DelegateCommand(UnSelectLastExecute, CanUnSelectLastExecute);
            IsInitializing = true;
        }

        public void Search(string searchText)
        {
            Snippets.Clear();
            var normalizedSearchText = SearchTokenizer.Tokenize(searchText?.ToLowerInvariant());
            var searchResult = searcher.Search(normalizedSearchText, Selected.LastOrDefault());
            Snippets.AddRange(searchResult);
            SearchCompleted.Invoke();
        }

        private bool CanSelectExecute(SnippetModel snippet)
        {
            var hasChildren = snippetTree.HasChildren(snippet);
            if (!hasChildren)
                return false;

            var parent = snippetTree.FindParent(snippet);

            if (Selected.Count == 0 || parent == null)
                return true;

            var lastSelected = Selected.Last();
            while (parent != null)
            {
                if (lastSelected == parent)
                    return true;

                parent = snippetTree.FindParent(parent);
            }

            return false;
        }

        private void SelectExecute(SnippetModel snippet)
        {
            var ancestors = snippetTree.GetAncestors(snippet, Selected.LastOrDefault());

            foreach(var ancestor in ancestors)
                Selected.Add(ancestor);

            Selected.Add(snippet);
            Search(string.Empty);
            UnSelectLast.RaiseCanExecuteChanged();
        }

        private bool CanUnSelectLastExecute()
            => Selected.Count > 0;

        private void UnSelectLastExecute()
        {
            Selected.Remove(Selected.Last());
            Search(string.Empty);
            UnSelectLast.RaiseCanExecuteChanged();
        }

    }
}
