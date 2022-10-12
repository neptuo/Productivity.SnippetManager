using Neptuo.Observables;
using Neptuo.Observables.Collections;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.ViewModels.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Neptuo.Productivity.SnippetManager.ViewModels
{
    public class MainViewModel : ObservableModel
    {
        private const int PageSize = 5;

        public ObservableCollection<SnippetModel> Snippets { get; } = new ObservableCollection<SnippetModel>();

        public ApplySnippetCommand Apply { get; init; }
        public CopySnippetCommand Copy { get; init; }

        private string[]? normalizedSearchText;

        private int searchResultCount = 0;

        public void Search(string searchText)
        {
            normalizedSearchText = searchText?.ToLowerInvariant().Split(' ');

            ICollectionView view = CollectionViewSource.GetDefaultView(Snippets);
            if (view.Filter == null)
            {
                view.SortDescriptions.Add(new SortDescription(nameof(SnippetModel.Priority), ListSortDirection.Ascending));
                view.SortDescriptions.Add(new SortDescription(nameof(SnippetModel.Title), ListSortDirection.Ascending));
                view.Filter = OnFilter;
            }

            searchResultCount = 0;
            view.Refresh();
        }

        private bool OnFilter(object item)
        {
            if (searchResultCount >= PageSize)
                return false;

            SnippetModel snippet = (SnippetModel)item;
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
