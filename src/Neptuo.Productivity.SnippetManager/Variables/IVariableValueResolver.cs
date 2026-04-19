namespace Neptuo.Productivity.SnippetManager.Variables;

public interface IVariableValueResolver
{
    bool TryGetValue(string name, out string? value);
}
