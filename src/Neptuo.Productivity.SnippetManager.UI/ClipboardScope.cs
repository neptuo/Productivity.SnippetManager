using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Neptuo.Productivity.SnippetManager;

public struct ClipboardScope
{
    private int format;
    private object? data;

    public ClipboardScope()
        => Store();

    public void Store()
    {
        if (Clipboard.ContainsText())
        {
            format = 1;
            data = Clipboard.GetText();
        }
        else if (Clipboard.ContainsAudio())
        {
            format = 2;
            data = Clipboard.GetAudioStream();
        }
        else if (Clipboard.ContainsImage())
        {
            format = 3;
            data = Clipboard.GetImage();
        }
        else if (Clipboard.ContainsFileDropList())
        {
            format = 4;
            data = Clipboard.GetFileDropList();
        }
        else
        {
            format = 0;
        }
    }

    public void Restore()
    {
        if (format == 0 || data == null)
        {
            Clipboard.Clear();
        }
        else
        {
            switch (format)
            {
                case 1:
                    Clipboard.SetText((string)data);
                    break;
                case 2:
                    Clipboard.SetAudio((Stream)data);
                    break;
                case 3:
                    Clipboard.SetImage((Image)data);
                    break;
                case 4:
                    Clipboard.SetFileDropList((StringCollection)data);
                    break;
                default:
                    throw Ensure.Exception.NotSupported($"Not supported format '{format}'.");
            }
        }
    }
}
