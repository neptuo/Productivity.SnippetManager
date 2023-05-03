using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
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
        private List<SnippetModel> searchResult = new();

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

        private const bool SupplyChildrenFromSelectedSnippets = false;
        private const bool SupplyNonRootSnippets = true;

        private void SearchNormalizedText()
        {
            Snippets.Clear();
            searchResult.Clear();

            SearchFromCurrentTopNode(false);

            if (SupplyChildrenFromSelectedSnippets)
            {
                // TODO: If we don't have enough items, you should include some children from the first match
                // Not sure at the moment, user can always use TAB to pin
                List<SnippetModel> toAdd = new();
                if (searchResult.Count < PageSize)
                {
                    foreach (var snippet in searchResult)
                    {
                        foreach (var child in snippetTree.GetChildren(snippet))
                        {
                            toAdd.Add(child);

                            if (searchResult.Count + toAdd.Count >= PageSize)
                                break;
                        }

                        if (searchResult.Count + toAdd.Count >= PageSize)
                            break;
                    }
                }

                searchResult.AddRange(toAdd);
            }

            if (SupplyNonRootSnippets)
            {
                // If we have zero items, we should go in depth first
                if (searchResult.Count == 0)
                    SearchFromCurrentTopNode(true);
            }

            Snippets.AddRange(searchResult.OrderBy(m => m.Priority).ThenBy(m => m.Title));
        }

        private void SearchFromCurrentTopNode(bool goInDepth)
        {
            SnippetModel? parent = Selected.LastOrDefault();
            var children = parent == null
                ? snippetTree.GetRoots()
                : snippetTree.GetChildren(parent);

            SearchTree(children, 0, goInDepth);
        }

        private void SearchTree(IEnumerable<SnippetModel> snippets, int fromIndex, bool goInDepth)
        {
            foreach (var snippet in snippets.OrderBy(m => m.Priority).ThenBy(m => m.Title))
            {
                if (searchResult.Count >= PageSize)
                    break;

                int lastMatchedIndex = IsFilterPassed(snippet, fromIndex);
                if (lastMatchedIndex == normalizedSearchText!.Count - 1)
                {
                    searchResult.Add(snippet);
                    continue;
                }
                else if (lastMatchedIndex >= 0 || (goInDepth && lastMatchedIndex == -1))
                {
                    var children = snippetTree.GetChildren(snippet);
                    SearchTree(children, lastMatchedIndex + 1, goInDepth);
                }
                else
                {
                    Debug.Assert(true, "Unreachable code");
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
            for (int i = fromIndex; i < normalizedSearchText.Count; i++)
            {
                int currentIndex = pathMatch.IndexOf(normalizedSearchText[i]);
                if (currentIndex == -1)
                    break;

                result = i;
                pathMatch = pathMatch.Substring(currentIndex + normalizedSearchText[i].Length);
            }

            return result;
        }
    }
}
