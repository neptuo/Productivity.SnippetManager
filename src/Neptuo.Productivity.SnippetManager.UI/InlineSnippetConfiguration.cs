﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class InlineSnippetConfiguration : Dictionary<string, string>, IProviderConfiguration<InlineSnippetConfiguration>
{
    public static InlineSnippetConfiguration Example => new()
    {
        ["Hello"] = "Hello, World!"
    };
}
