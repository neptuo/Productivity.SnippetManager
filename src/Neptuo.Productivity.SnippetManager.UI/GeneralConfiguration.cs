using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class GeneralConfiguration
{
    public string? HotKey { get; set; }

    public static GeneralConfiguration Example = new GeneralConfiguration()
    {
        HotKey = "Control+Shift+V"
    };
}
