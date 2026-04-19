using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Neptuo.Productivity.SnippetManager
{
    public class ProviderConfiguration : IEquatable<ProviderConfiguration>, IProviderConfiguration<ProviderConfiguration>
    {
        [DefaultValue(true)]
        [JsonPropertyName("Enabled")]
        public bool IsEnabled { get; set; } = true;

        public static ProviderConfiguration Example => new();

        public bool Equals(ProviderConfiguration? other) 
            => other != null && IsEnabled == other.IsEnabled;
    }
}
