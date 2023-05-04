using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace Neptuo.Productivity.SnippetManager;

public class SnippetSearcher
{
    private const bool SupplyChildrenFromSelectedSnippets = true;
    private const bool SupplyNonRootSnippets = true;
    private readonly ISnippetTree snippetTree;
    private readonly int pageSize;

    public SnippetSearcher(ISnippetTree snippetTree, int pageSize)
    {
        this.snippetTree = snippetTree;
        this.pageSize = pageSize;
    }

    public IEnumerable<SnippetModel> Search(IReadOnlyList<string>? normalizedSearchText, SnippetModel? currentRoot)
    {
        List<SnippetModel> searchResult = new();

        searchResult.Clear();

        SearchFromCurrentTopNode(searchResult, currentRoot, normalizedSearchText, false);

        if (SupplyChildrenFromSelectedSnippets)
        {
            // TODO: If we don't have enough items, you should include some children from the first match
            // Not sure at the moment, user can always use TAB to pin
            List<SnippetModel> toAdd = new();
            if (searchResult.Count < pageSize)
            {
                foreach (var snippet in searchResult)
                {
                    foreach (var child in snippetTree.GetChildren(snippet))
                    {
                        toAdd.Add(child);

                        if (searchResult.Count + toAdd.Count >= pageSize)
                            break;
                    }

                    if (searchResult.Count + toAdd.Count >= pageSize)
                        break;
                }
            }

            searchResult.AddRange(toAdd);
        }

        if (SupplyNonRootSnippets && currentRoot == null)
        {
            // If we have zero items, we should go in depth first
            if (searchResult.Count == 0)
                SearchFromCurrentTopNode(searchResult, null, normalizedSearchText, true);
        }

        // TODO: searchResult.Count == 0 && we have parent

        return searchResult.OrderBy(m => m.Priority).ThenBy(m => m.Title);
    }

    private void SearchFromCurrentTopNode(List<SnippetModel> searchResult, SnippetModel? currentRoot, IReadOnlyList<string>? normalizedSearchText, bool goInDepth)
    {
        var children = currentRoot == null
            ? snippetTree.GetRoots()
            : snippetTree.GetChildren(currentRoot);

        SearchTree(searchResult, children, normalizedSearchText, 0, goInDepth);
    }

    private void SearchTree(List<SnippetModel> searchResult, IEnumerable<SnippetModel> snippets, IReadOnlyList<string>? normalizedSearchText, int fromIndex, bool goInDepth)
    {
        foreach (var snippet in snippets.OrderBy(m => m.Priority).ThenBy(m => m.Title))
        {
            if (searchResult.Count >= pageSize)
                break;

            int lastMatchedIndex = IsFilterPassed(snippet, normalizedSearchText, fromIndex);
            if (lastMatchedIndex == normalizedSearchText!.Count - 1)
            {
                searchResult.Add(snippet);
                continue;
            }
            else if (lastMatchedIndex >= 0 || (goInDepth && lastMatchedIndex == -1))
            {
                var children = snippetTree.GetChildren(snippet);
                SearchTree(searchResult, children, normalizedSearchText, lastMatchedIndex + 1, goInDepth);
            }
            else
            {
                Debug.Assert(true, "Unreachable code");
            }
        }
    }

    private int IsFilterPassed(SnippetModel snippet, IReadOnlyList<string>? normalizedSearchText, int fromIndex)
    {
        if (normalizedSearchText!.Count == 0)
            return snippet.Priority >= SnippetPriority.High ? 0 : -1;

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
