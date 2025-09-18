using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class ApplySnippetCommand(ISendTextService sendText) : UseSnippetCommand
    {
        public override void Execute(SnippetModel parameter)
            => sendText.Send(parameter.Text);
    }
}
