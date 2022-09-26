using Neptuo.Observables.Commands;
using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager.ViewModels.Commands
{
    public abstract class UseSnippetCommand : Command<IAppliableSnippetModel>
    {
        public override bool CanExecute(IAppliableSnippetModel parameter) 
            => parameter.IsFilled;

        public override void Execute(IAppliableSnippetModel parameter)
        {
            throw new NotImplementedException();
        }
    }
}
