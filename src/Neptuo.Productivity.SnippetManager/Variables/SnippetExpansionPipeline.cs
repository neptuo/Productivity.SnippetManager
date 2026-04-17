namespace Neptuo.Productivity.SnippetManager.Variables;

public class SnippetExpansionPipeline(
    ISnippetVariableScanner scanner,
    IVariableValueResolver resolver,
    ISnippetTextExpander expander)
{
    public string Apply(string text)
    {
        var references = scanner.Scan(text);
        if (references.Count == 0)
            return text;

        var values = new Dictionary<string, string?>();
        foreach (var reference in references)
        {
            resolver.TryGetValue(reference.Name, out var value);
            values[reference.Name] = value;
        }

        return expander.Expand(text, values);
    }
}
