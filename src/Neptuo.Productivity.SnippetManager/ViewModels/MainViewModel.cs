using Neptuo.Observables;
using Neptuo.Observables.Collections;
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

        public ObservableCollection<SnippetModel> Snippets { get; } = new ObservableCollection<SnippetModel>();

        public ApplySnippetCommand Apply { get; }
        public CopySnippetCommand Copy { get; }

        private string[]? normalizedSearchText;
        private ICollection<SnippetModel> allSnippets;
        private int searchResultCount = 0;

        public MainViewModel(ICollection<SnippetModel> allSnippets, ApplySnippetCommand apply, CopySnippetCommand copy)
        {
            this.allSnippets = allSnippets;
            Apply = apply;
            Copy = copy;
        }

        public void Search(string searchText)
        {
            normalizedSearchText = searchText?.ToLowerInvariant().Split(' ');
            SearchNormalizedText();
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
            if (normalizedSearchText == null)
                return true;

            bool result = true;
            string pathMatch = snippet.Title.ToLowerInvariant();
            for (int i = 0; i < normalizedSearchText.Length; i++)
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
