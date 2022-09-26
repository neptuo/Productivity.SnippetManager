using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Models
{
    public interface IAppliableSnippetModel
    {
        string Title { get; }
        string Text { get; }
        bool IsFilled { get; }
    }
}
