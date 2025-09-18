using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class CopySnippetCommand : UseSnippetCommand
    {
        private readonly IClipboardService clipboard;

        public CopySnippetCommand(IClipboardService clipboard) 
            => this.clipboard = clipboard;

        public override void Execute(SnippetModel parameter)
            => clipboard.SetText(parameter.Text);
    }
}
