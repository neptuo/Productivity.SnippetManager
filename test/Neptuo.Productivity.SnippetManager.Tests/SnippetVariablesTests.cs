using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SnippetVariablesTests
{
    #region Scanner tests

    [Fact]
    public void Scanner_ReturnsEmptyForTextWithNoTokens()
    {
        var scanner = new TokenSnippetVariableScanner();
        var result = scanner.Scan("Hello, World!");
        Assert.Empty(result);
    }

    [Fact]
    public void Scanner_ReturnsEmptyForEmptyText()
    {
        var scanner = new TokenSnippetVariableScanner();
        var result = scanner.Scan(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void Scanner_ReturnsDistinctNames()
    {
        var scanner = new TokenSnippetVariableScanner();
        var result = scanner.Scan("{A} and {A} and {B}");
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Name == "A");
        Assert.Contains(result, r => r.Name == "B");
    }

    [Fact]
    public void Scanner_ReturnsSingleToken()
    {
        var scanner = new TokenSnippetVariableScanner();
        var result = scanner.Scan("Hello, {Name}!");
        Assert.Single(result);
        Assert.Equal("Name", result[0].Name);
    }

    [Fact]
    public void Scanner_ReturnsEmptyForMalformedInput()
    {
        var scanner = new TokenSnippetVariableScanner();
        // JSON-like input causes parse failure
        var result = scanner.Scan("{\"key\": \"value\"}");
        Assert.Empty(result);
    }

    [Fact]
    public void Scanner_ReturnsEmptyForAttributeToken()
    {
        var scanner = new TokenSnippetVariableScanner();
        // Attributes disabled → parse fails
        var result = scanner.Scan("{Name key=value}");
        Assert.Empty(result);
    }

    [Fact]
    public void Scanner_DoesNotReturnEscapedTokens()
    {
        var scanner = new TokenSnippetVariableScanner();
        var result = scanner.Scan("{{escaped}} and {Real}");
        Assert.Single(result);
        Assert.Equal("Real", result[0].Name);
    }

    #endregion

    #region Expander tests

    [Fact]
    public void Expander_ReplacesKnownToken()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        var result = expander.Expand("install.{ShellExt}", values);
        Assert.Equal("install.ps1", result);
    }

    [Fact]
    public void Expander_PassesThroughUnknownToken()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?>();
        var result = expander.Expand("install.{OtherExt}", values);
        Assert.Equal("install.{OtherExt}", result);
    }

    [Fact]
    public void Expander_PassesThroughNullValue()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?> { ["ShellExt"] = null };
        var result = expander.Expand("install.{ShellExt}", values);
        Assert.Equal("install.{ShellExt}", result);
    }

    [Fact]
    public void Expander_PreservesEscapeSequence()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        var result = expander.Expand("{{literal}} and {ShellExt}", values);
        Assert.Equal("{{literal}} and ps1", result);
    }

    [Fact]
    public void Expander_PreservesTextWithNoTokens()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?>();
        var result = expander.Expand("Hello, World!", values);
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Expander_ReturnsMalformedInputUnchanged()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?>();
        var result = expander.Expand("{\"key\": \"value\"}", values);
        Assert.Equal("{\"key\": \"value\"}", result);
    }

    [Fact]
    public void Expander_IsCaseSensitive()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        var result = expander.Expand("{shellext}", values);
        // "shellext" is not the same as "ShellExt" → pass through
        Assert.Equal("{shellext}", result);
    }

    [Fact]
    public void Expander_ReplacesMultipleTokens()
    {
        var expander = new TokenSnippetTextExpander();
        var values = new Dictionary<string, string?> { ["A"] = "hello", ["B"] = "world" };
        var result = expander.Expand("{A} and {B}", values);
        Assert.Equal("hello and world", result);
    }

    #endregion

    #region Resolver tests

    [Fact]
    public void ConfigurationResolver_ReturnsTrueForKnownName()
    {
        var config = new VariablesConfiguration { ["ShellExt"] = "sh" };
        var resolver = new ConfigurationVariableValueResolver(config);
        bool found = resolver.TryGetValue("ShellExt", out var value);
        Assert.True(found);
        Assert.Equal("sh", value);
    }

    [Fact]
    public void ConfigurationResolver_ReturnsFalseForUnknownName()
    {
        var config = new VariablesConfiguration { ["ShellExt"] = "sh" };
        var resolver = new ConfigurationVariableValueResolver(config);
        bool found = resolver.TryGetValue("Other", out var value);
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void ConfigurationResolver_ReturnsFalseForNullConfiguration()
    {
        var resolver = new ConfigurationVariableValueResolver(null);
        bool found = resolver.TryGetValue("ShellExt", out var value);
        Assert.False(found);
        Assert.Null(value);
    }

    [Fact]
    public void CompositeResolver_UsesFirstMatchWins()
    {
        var first = new VariablesConfiguration { ["ShellExt"] = "sh" };
        var second = new VariablesConfiguration { ["ShellExt"] = "ps1", ["Other"] = "x" };
        var composite = new CompositeVariableValueResolver(new IVariableValueResolver[]
        {
            new ConfigurationVariableValueResolver(first),
            new ConfigurationVariableValueResolver(second)
        });

        composite.TryGetValue("ShellExt", out var shellExt);
        composite.TryGetValue("Other", out var other);

        Assert.Equal("sh", shellExt);   // first wins
        Assert.Equal("x", other);       // only in second
    }

    #endregion

    #region Pipeline end-to-end tests

    [Fact]
    public void Pipeline_ExpandsVariables()
    {
        var config = new VariablesConfiguration { ["ShellExt"] = "ps1" };
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetVariableScanner(),
            new ConfigurationVariableValueResolver(config),
            new TokenSnippetTextExpander()
        );

        var result = pipeline.Apply("Run install.{ShellExt}");
        Assert.Equal("Run install.ps1", result);
    }

    [Fact]
    public void Pipeline_PassesThroughTextWithNoTokens()
    {
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetVariableScanner(),
            new ConfigurationVariableValueResolver(null),
            new TokenSnippetTextExpander()
        );

        var result = pipeline.Apply("Hello, World!");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Pipeline_PassesThroughUnknownTokens()
    {
        var config = new VariablesConfiguration { ["ShellExt"] = "ps1" };
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetVariableScanner(),
            new ConfigurationVariableValueResolver(config),
            new TokenSnippetTextExpander()
        );

        var result = pipeline.Apply("install.{OtherExt}");
        Assert.Equal("install.{OtherExt}", result);
    }

    [Fact]
    public void Pipeline_DollarPrefixExpandsWhenVariableDefined()
    {
        // ${PATH} — the $ is plain text content before the {PATH} token.
        // When PATH is defined, {PATH} expands and $ is preserved as-is,
        // yielding "export $" + resolved-value = "export $/usr/local/bin".
        var config = new VariablesConfiguration { ["PATH"] = "/usr/local/bin" };
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetVariableScanner(),
            new ConfigurationVariableValueResolver(config),
            new TokenSnippetTextExpander()
        );

        var result = pipeline.Apply("export ${PATH}");
        Assert.Equal("export $/usr/local/bin", result);
    }

    [Fact]
    public void Pipeline_DollarPrefixPassesThroughWhenVariableNotDefined()
    {
        // ${PATH} — PATH not defined → pass through verbatim
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetVariableScanner(),
            new ConfigurationVariableValueResolver(null),
            new TokenSnippetTextExpander()
        );

        var result = pipeline.Apply("export ${PATH}");
        Assert.Equal("export ${PATH}", result);
    }

    #endregion
}
