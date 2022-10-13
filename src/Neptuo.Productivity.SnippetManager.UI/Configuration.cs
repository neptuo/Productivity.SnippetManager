﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class Configuration
    {
        public ProviderConfiguration? Clipboard { get; set; }
        public ProviderConfiguration? Guid { get; set; }
        public GitHubConfiguration? GitHub { get; set; }
        public XmlConfiguration? Xml { get; set; }
    }
}
