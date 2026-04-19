using System.Diagnostics;

namespace Neptuo.Productivity.SnippetManager.Models
{
    [DebuggerDisplay("Snippet {Title}")]
    public sealed class SnippetModel : IAppliableSnippetModel
    {
        public static string PathSeparator = " - ";
        private static string[] lineSeparators = new[] { Environment.NewLine, "\n" };

        public string[] Path { get; }

        public string Title { get; }
        public string? Description { get; }
        public string Text { get; }

        public int Priority { get; }

        public bool IsShadow { get; }
        public bool IsFilled { get; }

        public SnippetModel(string title, int priority = SnippetPriority.Normal)
        {
            IsFilled = false;
            IsShadow = true;

            Title = title;
            Text = string.Empty;
            Priority = priority;

            // TODO: This snippet is in fact unappliable

            Path = Title.Split(PathSeparator);
        }

        public SnippetModel(string title, string text, string? description = null, int priority = SnippetPriority.Normal)
        {
            IsFilled = true;

            Title = title;
            Text = text;
            Description = description;
            Priority = priority;

            if (description == null)
            {
                string[] lines = text.Split(lineSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(lines.Length > 0);
                Description = lines[0] + (lines.Length > 1 ? "..." : string.Empty);
            }

            Path = Title.Split(PathSeparator);
        }
    }
}
