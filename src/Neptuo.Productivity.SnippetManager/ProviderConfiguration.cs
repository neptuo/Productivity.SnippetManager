using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class ProviderConfiguration
    {
        [DefaultValue(true)]
        [JsonPropertyName("Enabled")]
        public bool IsEnabled { get; set; } = true;

        public static ProviderConfiguration Example => new();
    }
}
