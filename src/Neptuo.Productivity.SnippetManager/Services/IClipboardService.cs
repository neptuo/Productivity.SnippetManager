using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Services
{
    public interface IClipboardService
    {
        void SetText(string text);
    }
}
