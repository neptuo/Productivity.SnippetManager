namespace Neptuo.Productivity.SnippetManager.Variables;

public class CompositeVariableValueResolver(IEnumerable<IVariableValueResolver> resolvers) : IVariableValueResolver
{
    public bool TryGetValue(string name, out string? value)
    {
        foreach (var resolver in resolvers)
        {
            if (resolver.TryGetValue(name, out value))
                return true;
        }

        value = null;
        return false;
    }
}
