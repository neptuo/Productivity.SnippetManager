namespace Neptuo.Productivity.SnippetManager.Variables;

public class SnippetExpansionPipeline(
    ISnippetTemplateCompiler compiler,
    IVariableValueResolver resolver)
{
    public string Apply(string text)
    {
        var template = compiler.Compile(text);
        if (template.Variables.Count == 0)
            return text;

        var values = new Dictionary<string, string?>();
        foreach (var reference in template.Variables)
        {
            resolver.TryGetValue(reference.Name, out var value);
            values[reference.Name] = value;
        }

        return template.Render(values);
    }
}
