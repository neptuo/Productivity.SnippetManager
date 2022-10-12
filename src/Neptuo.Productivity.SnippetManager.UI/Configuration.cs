using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class Configuration
    {
        public GitHubConfiguration GitHub { get; set; } = new GitHubConfiguration();
    }
}
