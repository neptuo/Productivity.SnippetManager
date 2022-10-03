using Neptuo;
using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class CopySnippetCommand : UseSnippetCommand
    {
        private readonly IClipboardService clipboard;

        public CopySnippetCommand(IClipboardService clipboard) 
            => this.clipboard = clipboard;

        public override void Execute(IAppliableSnippetModel parameter)
            => clipboard.SetText(parameter.Text);
    }
}
