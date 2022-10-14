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
        public GeneralConfiguration? General { get; set; }
        public ProviderConfiguration? Clipboard { get; set; }
        public ProviderConfiguration? Guid { get; set; }
        public GitHubConfiguration? GitHub { get; set; }
        public XmlConfiguration? Xml { get; set; }

        public static Configuration Example => new()
        {
            General = GeneralConfiguration.Example,
            Clipboard = ProviderConfiguration.Example,
            Guid = ProviderConfiguration.Example,
            GitHub = GitHubConfiguration.Example,
            Xml = XmlConfiguration.Example
        };
    }
}
