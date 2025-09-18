using System.Text.Json.Serialization;

namespace Neptuo.Productivity.SnippetManager
{
    public class Configuration
    {
        public GeneralConfiguration? General { get; set; }

        [JsonIgnore]
        public Dictionary<string, object> Providers { get; } = new();
    }
}
