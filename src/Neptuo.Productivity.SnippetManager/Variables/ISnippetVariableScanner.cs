namespace Neptuo.Productivity.SnippetManager.Variables;

public interface ISnippetVariableScanner
{
    IReadOnlyList<VariableReference> Scan(string text);
}
