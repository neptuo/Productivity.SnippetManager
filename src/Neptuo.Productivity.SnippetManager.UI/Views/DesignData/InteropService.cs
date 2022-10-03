using Neptuo.Productivity.SnippetManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.Views.DesignData;

internal class InteropService : IClipboardService, ISendTextService
{
    public static readonly InteropService Instance = new InteropService();

    public void Send(string text)
    {
    }

    public void SetText(string text)
    {
    }
}
