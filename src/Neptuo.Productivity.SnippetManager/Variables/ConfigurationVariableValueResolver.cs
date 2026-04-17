namespace Neptuo.Productivity.SnippetManager.Variables;

public class ConfigurationVariableValueResolver(VariablesConfiguration? configuration) : IVariableValueResolver
{
    public bool TryGetValue(string name, out string? value)
    {
        if (configuration != null && configuration.TryGetValue(name, out value))
            return true;

        value = null;
        return false;
    }
}
