namespace Neptuo.Productivity.SnippetManager
{
    public interface ISnippetProviderFactory<T>
    {
        bool TryCreate(T? configuration, out ISnippetProvider? provider);
    }
}
