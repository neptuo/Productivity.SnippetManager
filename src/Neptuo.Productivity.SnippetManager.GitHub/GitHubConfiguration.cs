using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class GitHubConfiguration : ProviderConfiguration
    {
        public string? UserName { get; set; }
        public string? AccessToken { get; set; }
        public List<string>? ExtraRepositories { get; set; }
    }
}
