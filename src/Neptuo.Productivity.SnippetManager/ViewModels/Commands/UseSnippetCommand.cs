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
    public abstract class UseSnippetCommand : Command<SnippetModel>
    {
        public override bool CanExecute(SnippetModel parameter) 
            => parameter.IsFilled;

        public override void Execute(SnippetModel parameter)
        {
            throw new NotImplementedException();
        }
    }
}
