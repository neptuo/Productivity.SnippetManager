using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class Configuration
    {
        public GeneralConfiguration? General { get; set; }

        [JsonIgnore]
        public Dictionary<string, object> Providers { get; } = new();
    }
}
