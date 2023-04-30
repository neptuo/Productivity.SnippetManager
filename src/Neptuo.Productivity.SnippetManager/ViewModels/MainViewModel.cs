using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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
        private ICollection<SnippetModel> allSnippets;
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

        public MainViewModel(ICollection<SnippetModel> allSnippets, ApplySnippetCommand apply, CopySnippetCommand copy)
        {
            this.allSnippets = allSnippets;
            Apply = apply;
            Copy = copy;
            Select = new DelegateCommand<SnippetModel>(SelectExecute, CanSelectExecute);
            UnSelectLast = new DelegateCommand(UnSelectLastExecute, CanUnSelectLastExecute);
            IsInitializing = true;
        }

        public void Search(string searchText)
        {
            normalizedSearchText = SearchTokenizer.Tokenize(searchText?.ToLowerInvariant());
            if (normalizedSearchText != null && normalizedSearchText.Count == 1 && normalizedSearchText[0] == string.Empty)
                normalizedSearchText = null;

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
            foreach (var snippet in allSnippets.OrderBy(m => m.Priority).ThenBy(m => m.Title))
            {
                if (searchResultCount >= PageSize)
                    break;

                if (OnFilter(snippet))
                    Snippets.Add(snippet);
            }
        }

        private bool OnFilter(SnippetModel snippet)
        {
            var isPassed = IsFilterPassed(snippet);
            if (isPassed)
                searchResultCount++;

            return isPassed;
        }

        private bool IsFilterPassed(SnippetModel snippet)
        {
            SnippetModel? parent = Selected.LastOrDefault();
            if (parent == null && snippet.ParentId != null)
                return false;

            if (parent != null && !parent.Id.Equals(snippet.ParentId))
                return false;

            if (normalizedSearchText == null)
            {
                if (parent != null)
                    return true;

                return snippet.Priority <= SnippetPriority.High;
            }

            bool result = true;
            string pathMatch = snippet.Title.ToLowerInvariant();
            for (int i = 0; i < normalizedSearchText.Count; i++)
            {
                int currentIndex = pathMatch.IndexOf(normalizedSearchText[i]);
                if (currentIndex == -1)
                {
                    result = false;
                    break;
                }

                pathMatch = pathMatch.Substring(currentIndex + normalizedSearchText[i].Length);
            }

            return result;
        }
    }
}
