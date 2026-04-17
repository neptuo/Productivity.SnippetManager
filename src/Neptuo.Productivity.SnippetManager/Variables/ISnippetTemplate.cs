namespace Neptuo.Productivity.SnippetManager.Variables;

public interface ISnippetTemplate
{
    IReadOnlyList<VariableReference> Variables { get; }
    string Render(IReadOnlyDictionary<string, string?> values);
}
