using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class SnippetProviderContext
    {
        public ICollection<SnippetModel> Models { get; } = new List<SnippetModel>();
    }
}
