using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class ConfigurationRepository
{
    private readonly SnippetProviderCollection snippetProviders;
    private readonly JsonSerializerOptions options;

    public ConfigurationRepository(SnippetProviderCollection snippetProviders)
    {
        this.snippetProviders = snippetProviders;
        options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            {
                Modifiers = { TypeInfoModifier }
            }
        };
    }

    private void TypeInfoModifier(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object || typeInfo.Type != typeof(Configuration))
            return;

        foreach (var entry in snippetProviders.GetConfigurationMappings())
        {
            var property = typeInfo.CreateJsonPropertyInfo(entry.configurationType, entry.key);
            property.Get = c => ((Configuration)c).Providers.GetValueOrDefault(entry.key);
            property.Set = (c, value) =>
            {
                Configuration config = ((Configuration)c);
                if (value != null)
                    config.Providers[entry.key] = value;
                else
                    config.Providers.Remove(entry.key);
            };
            typeInfo.Properties.Add(property);
        }
    }

    public Configuration? Read(string filePath)
    {
        using var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);
        return JsonSerializer.Deserialize<Configuration>(fileStream, options);
    }

    public void Write(string filePath, Configuration configuration)
    {
        File.WriteAllText(filePath, JsonSerializer.Serialize(configuration, options: options));
    }
}
