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

        public Guid Id { get; } = Guid.NewGuid();
        public Guid? ParentId { get; }

        public string Title { get; }
        public string? Description { get; }
        public string Text { get; }

        public int Priority { get; }

        public bool IsFilled => true;

        public SnippetModel(string title, string text, string? description = null, int priority = SnippetPriority.Normal, Guid? parentId = null)
        {
            Title = title;
            Text = text;
            Description = description;
            Priority = priority;
            ParentId = parentId;

            if (description == null)
            {
                string[] lines = text.Split(lineSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Debug.Assert(lines.Length > 0);
                Description = lines[0] + (lines.Length > 1 ? "..." : string.Empty);
            }
        }
    }
}
