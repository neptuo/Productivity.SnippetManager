using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;

namespace Neptuo.Productivity.SnippetManager.ViewModels
{
    public class MainViewModel : ObservableModel
    {
        private const int PageSize = 5;

        public ObservableCollection<SnippetModel> Selected { get; } = new ObservableCollection<SnippetModel>();
        public ObservableCollection<SnippetModel> Snippets { get; } = new ObservableCollection<SnippetModel>();

        public ApplySnippetCommand Apply { get; }
        public CopySnippetCommand Copy { get; }
        public DelegateCommand<SnippetModel> Select { get; }
        public DelegateCommand UnSelectLast { get; }

        private IReadOnlyList<string>? normalizedSearchText;
        private ISnippetTree snippetTree;
        private int searchResultCount = 0;

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

        public MainViewModel(ISnippetTree snippetTree, ApplySnippetCommand apply, CopySnippetCommand copy)
        {
            this.snippetTree = snippetTree;
            Apply = apply;
            Copy = copy;
            Select = new DelegateCommand<SnippetModel>(SelectExecute, CanSelectExecute);
            UnSelectLast = new DelegateCommand(UnSelectLastExecute, CanUnSelectLastExecute);
            IsInitializing = true;
        }

        public void Search(string searchText)
        {
            normalizedSearchText = SearchTokenizer.Tokenize(searchText?.ToLowerInvariant());
            SearchNormalizedText();
        }

        private bool CanSelectExecute(SnippetModel snippet)
            => (Selected.Count == 0 && snippet.ParentId == null) || Selected.Last().Id == snippet.ParentId;

        private void SelectExecute(SnippetModel snippet)
        {
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

        private void SearchNormalizedText()
        {
            Snippets.Clear();

            searchResultCount = 0;

            SnippetModel? parent = Selected.LastOrDefault();
            var children = parent == null
                ? snippetTree.GetRoots()
                : snippetTree.GetChildren(parent);

            SearchTree(children, 0);
        }

        private void SearchTree(IEnumerable<SnippetModel> snippets, int fromIndex)
        {
            foreach (var snippet in snippets.OrderBy(m => m.Priority).ThenBy(m => m.Title))
            {
                if (searchResultCount >= PageSize)
                    break;

                int matchedIndex = IsFilterPassed(snippet, 0);
                if (matchedIndex == normalizedSearchText!.Count)
                {
                    Snippets.Add(snippet);
                    searchResultCount++;
                    continue;
                }
                else if (matchedIndex > 0)
                {
                    var children = snippetTree.GetChildren(snippet);
                    SearchTree(children, matchedIndex + 1);
                }
                else if (matchedIndex == -1)
                {
                    // TODO: Nothing matched, should we go in depth?
                }
            }
        }

        private int IsFilterPassed(SnippetModel snippet, int fromIndex)
        {
            // First pass: shallow from the top
            // Second pass: depth first

            // "maraf", "money"
            // GitHub - maraf - Money - issues

            if (normalizedSearchText!.Count == 0)
                return snippet.Priority <= SnippetPriority.High ? 0 : -1;

            int result = -1;
            string pathMatch = snippet.Title.ToLowerInvariant();
            for (int i = 0; i < normalizedSearchText.Count; i++)
            {
                int currentIndex = pathMatch.IndexOf(normalizedSearchText[i]);
                if (currentIndex == -1)
                {
                    result = i;
                    break;
                }

                pathMatch = pathMatch.Substring(currentIndex + normalizedSearchText[i].Length);
            }

            return result;
        }
    }
}
