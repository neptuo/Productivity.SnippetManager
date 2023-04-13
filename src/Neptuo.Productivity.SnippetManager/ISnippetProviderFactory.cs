using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public interface ISnippetProviderFactory<T>
    {
        bool TryCreate(T? configuration, out ISnippetProvider? provider);
    }
}
