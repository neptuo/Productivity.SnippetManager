using Neptuo.Productivity.SnippetManager.Models;
using System.Diagnostics;

namespace Neptuo.Productivity.SnippetManager;

public class SnippetSearcher
{
    private const bool SupplyChildrenFromSelectedSnippets = false;
    private const bool SupplyNonRootSnippets = true;

    private readonly ISnippetTree snippetTree;
    private readonly int pageSize;

    public SnippetSearcher(ISnippetTree snippetTree, int pageSize)
    {
        this.snippetTree = snippetTree;
        this.pageSize = pageSize;
    }

    public IEnumerable<SnippetModel> Search(IReadOnlyList<string> normalizedSearchText, SnippetModel? currentRoot)
    {
        List<SnippetModel> searchResult = new();

        searchResult.Clear();

        bool goInDepth = true;
        int fromIndex = 0;
        if (normalizedSearchText.Count > 0 && normalizedSearchText[0] == "^")
        {
            goInDepth = false;
            fromIndex = 1;
        }

        SearchTree(searchResult, currentRoot, normalizedSearchText, fromIndex, goInDepth);

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

        if (SupplyNonRootSnippets)
        {
            // If we have zero items, we should go in depth first
            if (searchResult.Count == 0)
                SearchTree(searchResult, currentRoot, normalizedSearchText, goInDepth: true);
        }

        // TODO: searchResult.Count == 0 && we have parent

        return searchResult.OrderBy(m => m.Priority).ThenBy(m => m.Title);
    }

    private void SearchTree(List<SnippetModel> searchResult, SnippetModel? parent, IReadOnlyList<string> normalizedSearchText, int fromIndex = 0, bool goInDepth = false)
    {
        var children = parent == null
            ? snippetTree.GetRoots()
            : snippetTree.GetChildren(parent);

        foreach (var snippet in children.OrderBy(m => m.Priority).ThenBy(m => m.Title))
        {
            if (searchResult.Count >= pageSize)
                break;

            if (normalizedSearchText.Count == 0)
            {
                if (snippet.Priority <= SnippetPriority.High || parent != null)
                    AddResult(searchResult, snippet);

                continue;
            }

            int lastMatchedIndex = IsFilterPassed(snippet, parent, normalizedSearchText, fromIndex);
            if (lastMatchedIndex >= normalizedSearchText!.Count - 1)
            {
                // Don't probe more when whole search phrase is matched
                AddResult(searchResult, snippet);
                continue;
            }
            else if (lastMatchedIndex >= fromIndex || (goInDepth && lastMatchedIndex == -1))
            {
                SearchTree(searchResult, snippet, normalizedSearchText, (goInDepth && lastMatchedIndex == -1) ? fromIndex : (lastMatchedIndex + 1), goInDepth);
            }
            else
            {
                Debug.Assert(true, "Unreachable code");
            }
        }
    }

    private static void AddResult(List<SnippetModel> searchResult, SnippetModel snippet) 
        => searchResult.Add(snippet);

    private int IsFilterPassed(SnippetModel snippet, SnippetModel? parent, IReadOnlyList<string> normalizedSearchText, int fromIndex)
    {
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
