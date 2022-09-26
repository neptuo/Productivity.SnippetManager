using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager;

public class DemoSnippetProvider : ISnippetProvider
{
    public Task<IReadOnlyCollection<SnippetModel>> GetAsync()
    {
        List<SnippetModel> result = new();

        void Add(string title, string text, string? description = null) => result.Add(new SnippetModel()
        {
            Title = title,
            Text = text,
            Description = description ?? text.Split(Environment.NewLine)[0] + "..."
        });

        Add(
            "C# class",
            """
            using System;
            using System.Collections.Generic;
            using System.IO;
            using System.Linq;
            using System.Text;
            using System.Threading.Tasks;

            namespace Namespace
            {
                public class Class
                {
                }
            }
            """,
            "public class Class..."
        );
        Add("Google", "https://google.com");
        Add("Maps", "https://maps.google.com");
        Add("GitHub - dotnet - runtime", "https://github.com/dotnet/runtime");
        Add("GitHub - Maraf - Money", "https://github.com/maraf/money");
        Add("GitHub - Neptuo - Recollections", "https://github.com/neptuo/Recollections");
        Add("Money", "https://app.money.neptuo.com");
        Add("Recollections", "https://recollections.app");
        Add("Signature", $"S pozdravem{Environment.NewLine}Marek Fišera");

        return Task.FromResult<IReadOnlyCollection<SnippetModel>>(result);
    }
}
