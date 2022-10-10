using Neptuo.Productivity.SnippetManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Neptuo.Productivity.SnippetManager;

public class XmlSnippetProvider : ISnippetProvider
{
    public Task InitializeAsync(SnippetProviderContext context)
    {
        string sourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "SnippetManager.xml");
        if (!File.Exists(sourcePath))
            return Task.CompletedTask;

        return Task.Run(() =>
        {
            XmlSnippetRoot? root;
            XmlSerializer serializer = new XmlSerializer(typeof(XmlSnippetRoot));
            using (FileStream sourceContent = new FileStream(sourcePath, FileMode.Open))
                root = (XmlSnippetRoot?)serializer.Deserialize(sourceContent);

            if (root == null || root.Snippets == null)
                return SnippetModel.EmptyCollection;

            List<SnippetModel> result = new();
            foreach (var snippet in root.Snippets)
            {
                string? text = snippet.TextAttribute ?? snippet.Text;
                if (text == null)
                    continue;

                string title = snippet.Title ?? text;
                result.Add(new SnippetModel(title, text));
            }

            return result;
        });
    }

    public Task UpdateAsync(SnippetProviderContext context)
        => Task.CompletedTask;
}
