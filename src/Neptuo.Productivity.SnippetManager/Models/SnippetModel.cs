using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Models
{
    [DebuggerDisplay("Snippet {Title}")]
    public class SnippetModel : IAppliableSnippetModel
    {
        private static string[] lineSeparators = new[] { Environment.NewLine, "\n" };

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
        }
    }
}
