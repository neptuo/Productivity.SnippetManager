using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

[XmlType("Include")]
public class XmlIncludeEntry
{
    [XmlAttribute("Path")]
    public string? Path { get; set; }
}
