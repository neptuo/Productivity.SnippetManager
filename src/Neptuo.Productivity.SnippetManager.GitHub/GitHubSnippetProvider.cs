using Neptuo.Productivity.SnippetManager.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class GitHubSnippetProvider : ISnippetProvider
    {
        private const string UserName = "maraf";

        public async Task<IReadOnlyCollection<SnippetModel>> GetAsync()
        {
            List<SnippetModel> result = new();
            await LoadAsync(result);
            return result;
        }

        private async Task LoadAsync(ICollection<SnippetModel> snippets)
        {
            var github = new GitHubClient(new ProductHeaderValue("SnippetMananger"));
            var repositories = await github.Repository.GetAllForUser(UserName);

            foreach (var repository in repositories)
            {
                snippets.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name}",
                    text: repository.HtmlUrl
                ));
                snippets.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name} - Issues",
                    text: repository.HtmlUrl + "/issues"
                ));
                snippets.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name} - Issues - New",
                    text: repository.HtmlUrl + "/issues/new"
                ));
            }
        }
    }
}
