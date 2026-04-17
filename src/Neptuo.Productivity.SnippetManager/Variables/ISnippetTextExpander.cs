namespace Neptuo.Productivity.SnippetManager.Variables;

public interface ISnippetTextExpander
{
    string Expand(string text, IReadOnlyDictionary<string, string?> values);
}
