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
    public class ApplySnippetCommand : UseSnippetCommand
    {
        private readonly ISendTextService sendText;

        public ApplySnippetCommand(ISendTextService sendText) 
            => this.sendText = sendText;

        public override void Execute(IAppliableSnippetModel parameter)
            => sendText.Send(parameter.Text);
    }
}
