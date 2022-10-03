﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Models
{
    public class SnippetModel : IAppliableSnippetModel
    {
        private static string[] lineSeparators = new[] { Environment.NewLine, "\n" };

        public string Title { get; }
        public string? Description { get; }
        public string Text { get; }

        public int Priority { get; }

        public bool IsFilled => true;

        public SnippetModel(string title, string text, string? description = null, int priority = SnippetPriority.Normal)
        {
            Title = title;
            Text = text;
            Priority = priority;
            Description = description;

            if (description == null)
            {
                string[] lines = text.Split(lineSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                Description = lines[0] + (lines.Length > 1 ? "..." : string.Empty);
            }
        }

        public static IReadOnlyCollection<SnippetModel> EmptyCollection { get; } = Array.Empty<SnippetModel>();

        public static IReadOnlyCollection<SnippetModel> SingleCollection(SnippetModel model) 
            => new[] { model };

        public static IReadOnlyCollection<SnippetModel> SingleCollection(string title, string text, string? description = null, int priority = SnippetPriority.Normal) 
            => SingleCollection(new SnippetModel(title, text, description, priority));
    }
}
