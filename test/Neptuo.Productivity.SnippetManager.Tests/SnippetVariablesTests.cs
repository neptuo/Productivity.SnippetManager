using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager.Tests;

public class SnippetVariablesTests
{
    #region Compiler tests

    [Fact]
    public void Compiler_ReturnsEmptyVariablesForTextWithNoTokens()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile("Hello, World!");
        Assert.Empty(template.Variables);
    }

    [Fact]
    public void Compiler_ReturnsEmptyVariablesForEmptyText()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile(string.Empty);
        Assert.Empty(template.Variables);
    }

    [Fact]
    public void Compiler_ReturnsDistinctVariableNames()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile("{A} and {A} and {B}");
        Assert.Equal(2, template.Variables.Count);
        Assert.Contains(template.Variables, r => r.Name == "A");
        Assert.Contains(template.Variables, r => r.Name == "B");
    }

    [Fact]
    public void Compiler_ReturnsSingleVariable()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile("Hello, {Name}!");
        Assert.Single(template.Variables);
        Assert.Equal("Name", template.Variables[0].Name);
    }

    [Fact]
    public void Compiler_ReturnsEmptyVariablesForMalformedInput()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        // JSON-like input causes parse failure
        var template = compiler.Compile("{\"key\": \"value\"}");
        Assert.Empty(template.Variables);
    }

    [Fact]
    public void Compiler_ReturnsEmptyVariablesForAttributeToken()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        // Attributes disabled → parse fails
        var template = compiler.Compile("{Name key=value}");
        Assert.Empty(template.Variables);
    }

    [Fact]
    public void Compiler_DoesNotIncludeEscapedTokensInVariables()
    {
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile("{{escaped}} and {Real}");
        Assert.Single(template.Variables);
        Assert.Equal("Real", template.Variables[0].Name);
    }

    [Fact]
    public void Compiler_ParsesTextOnlyOnce()
    {
        // Compile once; Render many times on the same template reuses the parsed token positions.
        var compiler = new TokenSnippetTemplateCompiler();
        var template = compiler.Compile("install.{ShellExt}");

        var first = template.Render(new Dictionary<string, string?> { ["ShellExt"] = "ps1" });
        var second = template.Render(new Dictionary<string, string?> { ["ShellExt"] = "sh" });

        Assert.Equal("install.ps1", first);
        Assert.Equal("install.sh", second);
    }

    #endregion

    #region Template.Render tests

    [Fact]
    public void Template_ReplacesKnownToken()
    {
        var template = Compile("install.{ShellExt}");
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        Assert.Equal("install.ps1", template.Render(values));
    }

    [Fact]
    public void Template_PassesThroughUnknownToken()
    {
        var template = Compile("install.{OtherExt}");
        var values = new Dictionary<string, string?>();
        Assert.Equal("install.{OtherExt}", template.Render(values));
    }

    [Fact]
    public void Template_PassesThroughNullValue()
    {
        var template = Compile("install.{ShellExt}");
        var values = new Dictionary<string, string?> { ["ShellExt"] = null };
        Assert.Equal("install.{ShellExt}", template.Render(values));
    }

    [Fact]
    public void Template_PreservesEscapeSequence()
    {
        var template = Compile("{{literal}} and {ShellExt}");
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        Assert.Equal("{{literal}} and ps1", template.Render(values));
    }

    [Fact]
    public void Template_PreservesTextWithNoTokens()
    {
        var template = Compile("Hello, World!");
        Assert.Equal("Hello, World!", template.Render(new Dictionary<string, string?>()));
    }

    [Fact]
    public void Template_ReturnsMalformedInputUnchanged()
    {
        var template = Compile("{\"key\": \"value\"}");
        Assert.Equal("{\"key\": \"value\"}", template.Render(new Dictionary<string, string?>()));
    }

    [Fact]
    public void Template_IsCaseSensitive()
    {
        var template = Compile("{shellext}");
        var values = new Dictionary<string, string?> { ["ShellExt"] = "ps1" };
        // "shellext" is not the same as "ShellExt" → pass through
        Assert.Equal("{shellext}", template.Render(values));
    }

    [Fact]
    public void Template_ReplacesMultipleTokens()
    {
        var template = Compile("{A} and {B}");
        var values = new Dictionary<string, string?> { ["A"] = "hello", ["B"] = "world" };
        Assert.Equal("hello and world", template.Render(values));
    }

    private static ISnippetTemplate Compile(string text)
        => new TokenSnippetTemplateCompiler().Compile(text);

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
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(config)
        );

        var result = pipeline.Apply("Run install.{ShellExt}");
        Assert.Equal("Run install.ps1", result);
    }

    [Fact]
    public void Pipeline_PassesThroughTextWithNoTokens()
    {
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(null)
        );

        var result = pipeline.Apply("Hello, World!");
        Assert.Equal("Hello, World!", result);
    }

    [Fact]
    public void Pipeline_PassesThroughUnknownTokens()
    {
        var config = new VariablesConfiguration { ["ShellExt"] = "ps1" };
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(config)
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
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(config)
        );

        var result = pipeline.Apply("export ${PATH}");
        Assert.Equal("export $/usr/local/bin", result);
    }

    [Fact]
    public void Pipeline_DollarPrefixPassesThroughWhenVariableNotDefined()
    {
        // ${PATH} — PATH not defined → pass through verbatim
        var pipeline = new SnippetExpansionPipeline(
            new TokenSnippetTemplateCompiler(),
            new ConfigurationVariableValueResolver(null)
        );

        var result = pipeline.Apply("export ${PATH}");
        Assert.Equal("export ${PATH}", result);
    }

    #endregion
}
