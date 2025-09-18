using Neptuo.Productivity.SnippetManager.Models;
using Neptuo.Productivity.SnippetManager.Services;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public class ApplySnippetCommand : UseSnippetCommand
    {
        private readonly ISendTextService sendText;

        public ApplySnippetCommand(ISendTextService sendText) 
            => this.sendText = sendText;

        public override void Execute(SnippetModel parameter)
            => sendText.Send(parameter.Text);
    }
}
