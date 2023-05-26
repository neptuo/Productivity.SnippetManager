using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SnippetSearcherTests
{
    public class JsonSnippetModel
    {
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Text { get; set; }
        public int Priority { get; set; }
    }

    private SnippetProviderContext snippetContext;
    private SnippetSearcher searcher;

    public SnippetSearcherTests()
    {
        snippetContext = new();
        searcher = new(snippetContext, 5);

        var jsonModels = JsonSerializer.Deserialize<JsonSnippetModel[]>(File.ReadAllText("GitHubSnippets.json"));
        var idMapping = new Dictionary<string, Guid>();

        var models = jsonModels.Select(m =>
        {
            Guid? parentId = null;
            if (m.ParentId != null)
                parentId = idMapping[m.ParentId];

            var s = new SnippetModel(m.Title, m.Text, m.Description, m.Priority, parentId);
            idMapping[m.Id] = s.Id;

            return s;
        }).ToList();
        snippetContext.AddRange(models);
    }

    [Fact]
    public void ContextReady() => Assert.Equal(309, snippetContext.Models.Count());

    private SnippetModel[] Search(SnippetModel? currentRoot, params string[] normalizedSearchText)
    {
        var result = searcher.Search(normalizedSearchText, currentRoot).ToArray();
        return result;
    }

    [Fact]
    public void NoParent_1Token_Root()
    {
        var result = Search(null, "git");
        Assert.Single(result);
    }

    [Fact]
    public void NoParent_1Token_Nested()
    {
        var result = Search(null, "mara");
        Assert.Single(result);
    }

    [Fact]
    public void NoParent_2Tokens_Root()
    {
        var result = Search(null, "g", "mara");
        Assert.Single(result);
    }

    [Fact]
    public void NoParent_2Tokens_Root_Skipped()
    {
        var result = Search(null, "g", "aspnet");
        Assert.Single(result);
    }

    [Fact]
    public void NoParent_2Tokens_Nested()
    {
        var result = Search(null, "mara", "aspnet");
        Assert.Single(result);
    }

    [Fact]
    public void NoParent_3Tokens_Root()
    {
        var result = Search(null, "g", "mara", "aspnet");
        Assert.Single(result);
    }
}
