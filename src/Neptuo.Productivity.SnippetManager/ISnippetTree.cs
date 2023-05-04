using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;

namespace Neptuo.Productivity.SnippetManager;

public interface ISnippetTree
{
    SnippetModel? FindById(Guid id);
    IEnumerable<SnippetModel> GetRoots();
    IEnumerable<SnippetModel> GetChildren(SnippetModel parent);

    /// <summary>
    /// Returns enumeration of ancestors from farthest or closest.
    /// </summary>
    IEnumerable<SnippetModel> GetAncestors(SnippetModel child, SnippetModel? lastAncestor = null);
}
