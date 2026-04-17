namespace Neptuo.Productivity.SnippetManager.Variables;

public interface ISnippetTemplateCompiler
{
    ISnippetTemplate Compile(string text);
}
