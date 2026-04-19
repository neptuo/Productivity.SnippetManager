using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

[XmlType("Snippet")]
[XmlRoot("Snippet")]
public class XmlSnippetEntry
{
    [XmlAttribute("Title")]
    public string? Title { get; set; }

    [XmlAttribute("Text")]
    public string? TextAttribute { get; set; }

    [XmlText]
    public string? Text { get; set; }

    [XmlAttribute]
    public XmlSnippetPriority Priority { get; set; } = XmlSnippetPriority.Normal;
}
