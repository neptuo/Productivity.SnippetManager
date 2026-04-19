using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager;

public interface ISnippetTree
{
    SnippetModel? FindParent(SnippetModel child);
    IEnumerable<SnippetModel> GetRoots();
    IEnumerable<SnippetModel> GetChildren(SnippetModel parent);
    bool HasChildren(SnippetModel parent);

    /// <summary>
    /// Returns enumeration of ancestors from farthest or closest.
    /// </summary>
    IEnumerable<SnippetModel> GetAncestors(SnippetModel child, SnippetModel? lastAncestor = null);
}
