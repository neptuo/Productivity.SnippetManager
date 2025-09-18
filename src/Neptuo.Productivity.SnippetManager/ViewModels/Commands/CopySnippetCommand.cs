using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class CopySnippetCommand(IClipboardService clipboard) : UseSnippetCommand
    {
        public override void Execute(SnippetModel parameter)
            => clipboard.SetText(parameter.Text);
    }
}
