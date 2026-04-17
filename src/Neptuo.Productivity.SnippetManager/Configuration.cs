using System.Text.Json.Serialization;
using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager;

public class Configuration
{
    public GeneralConfiguration? General { get; set; }

    public VariablesConfiguration? Variables { get; set; }

    [JsonIgnore]
    public Dictionary<string, object> Providers { get; } = new();
}
