using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

[XmlRoot("Snippets", Namespace = "http://schemas.neptuo.com/xsd/productivity/SnippetManager.xsd")]
public class XmlSnippetRoot
{
    [XmlElement("Snippet")]
    public List<XmlSnippetEntry>? Snippets { get; set; }
}
