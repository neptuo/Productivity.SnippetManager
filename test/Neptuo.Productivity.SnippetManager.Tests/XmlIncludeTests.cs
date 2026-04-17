using Neptuo.Productivity.SnippetManager;
using Neptuo.Productivity.SnippetManager.Models;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class XmlIncludeTests : IDisposable
{
    private readonly string testDir;

    public XmlIncludeTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), "SnippetManagerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(testDir))
            Directory.Delete(testDir, true);
    }

    private string CreateXmlFile(string fileName, string content)
    {
        string filePath = Path.Combine(testDir, fileName);
        string? dir = Path.GetDirectoryName(filePath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        File.WriteAllText(filePath, content);
        return filePath;
    }

    private async Task<(List<SnippetModel> snippets, IReadOnlyList<string> filePaths)> LoadSnippetsAsync(string rootPath)
    {
        var configuration = new XmlConfiguration { FilePath = rootPath };
        using var provider = new XmlSnippetProvider(configuration);
        var context = new SnippetProviderContext();
        await provider.InitializeAsync(context);
        return (context.Models.ToList(), provider.ResolvedFilePaths.ToList());
    }

    [Fact]
    public async Task NoIncludes_LoadsSnippetsNormally()
    {
        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Hello" Text="World" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Single(snippets);
        Assert.Equal("Hello", snippets[0].Title);
        Assert.Single(filePaths);
        Assert.Equal(Path.GetFullPath(rootPath), filePaths[0]);
    }

    [Fact]
    public async Task SingleInclude_LoadsSnippetsFromBothFiles()
    {
        CreateXmlFile("included.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Included" Text="From included file" />
            </Snippets>
            """);

        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Root" Text="From root file" />
              <Include Path="included.xml" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Equal(2, snippets.Count);
        Assert.Contains(snippets, s => s.Title == "Root");
        Assert.Contains(snippets, s => s.Title == "Included");
        Assert.Equal(2, filePaths.Count);
    }

    [Fact]
    public async Task RecursiveInclude_LoadsFromNestedFiles()
    {
        CreateXmlFile("level2.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Level2" Text="Deep snippet" />
            </Snippets>
            """);

        CreateXmlFile("level1.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Level1" Text="Mid snippet" />
              <Include Path="level2.xml" />
            </Snippets>
            """);

        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Root" Text="Root snippet" />
              <Include Path="level1.xml" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Equal(3, snippets.Count);
        Assert.Contains(snippets, s => s.Title == "Root");
        Assert.Contains(snippets, s => s.Title == "Level1");
        Assert.Contains(snippets, s => s.Title == "Level2");
        Assert.Equal(3, filePaths.Count);
    }

    [Fact]
    public async Task CycleDetection_DoesNotInfiniteLoop()
    {
        var rootPath = CreateXmlFile("a.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="A" Text="Snippet A" />
              <Include Path="b.xml" />
            </Snippets>
            """);

        CreateXmlFile("b.xml", $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="B" Text="Snippet B" />
              <Include Path="a.xml" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Equal(2, snippets.Count);
        Assert.Contains(snippets, s => s.Title == "A");
        Assert.Contains(snippets, s => s.Title == "B");
        Assert.Equal(2, filePaths.Count);
    }

    [Fact]
    public async Task RelativePathResolution_ResolvesFromIncludingFileDirectory()
    {
        Directory.CreateDirectory(Path.Combine(testDir, "sub"));

        CreateXmlFile("sub/nested.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Nested" Text="From subdir" />
            </Snippets>
            """);

        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Include Path="sub/nested.xml" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Single(snippets);
        Assert.Equal("Nested", snippets[0].Title);
        Assert.Equal(2, filePaths.Count);
        Assert.Contains(filePaths, p => p.EndsWith("nested.xml"));
    }

    [Fact]
    public async Task MissingIncludeFile_GracefullySkipped()
    {
        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Root" Text="Still works" />
              <Include Path="nonexistent.xml" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Single(snippets);
        Assert.Equal("Root", snippets[0].Title);
        Assert.Single(filePaths);
    }

    [Fact]
    public async Task EmptyIncludePath_GracefullySkipped()
    {
        var rootPath = CreateXmlFile("root.xml", """
            <?xml version="1.0" encoding="utf-8" ?>
            <Snippets xmlns="http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd">
              <Snippet Title="Root" Text="Still works" />
              <Include Path="" />
            </Snippets>
            """);

        var (snippets, filePaths) = await LoadSnippetsAsync(rootPath);

        Assert.Single(snippets);
        Assert.Equal("Root", snippets[0].Title);
    }
}
