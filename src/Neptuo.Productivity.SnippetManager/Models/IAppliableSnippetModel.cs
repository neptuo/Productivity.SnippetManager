namespace Neptuo.Productivity.SnippetManager.Models
{
    public interface IAppliableSnippetModel
    {
        string Title { get; }
        string Text { get; }
        bool IsFilled { get; }
    }
}
