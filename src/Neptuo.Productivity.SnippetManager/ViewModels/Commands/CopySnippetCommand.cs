using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class CopySnippetCommand(IClipboardService clipboard, SnippetExpansionPipeline pipeline) : UseSnippetCommand
    {
        public override void Execute(SnippetModel parameter)
            => clipboard.SetText(pipeline.Apply(parameter.Text));
    }
}
