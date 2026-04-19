using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;
using Neptuo.Productivity.SnippetManager.Variables;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class ApplySnippetCommand(ISendTextService sendText, SnippetExpansionPipeline pipeline) : UseSnippetCommand
    {
        public override void Execute(SnippetModel parameter)
            => sendText.Send(pipeline.Apply(parameter.Text));
    }
}
