using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public interface ISnippetProvider
    {
        Task<IReadOnlyCollection<SnippetModel>> GetAsync();
    }
}
