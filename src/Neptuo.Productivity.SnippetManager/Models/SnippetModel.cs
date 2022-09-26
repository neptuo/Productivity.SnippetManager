using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Models
{
    public class SnippetModel : IAppliableSnippetModel
    {
        public string Title { get; init; }
        public string? Description { get; init; }

        public string Text { get; init; }
        public bool IsFilled => true;
    }
}
