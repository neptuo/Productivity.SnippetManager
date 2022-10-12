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
        private readonly GitHubConfiguration configuration;

        public GitHubSnippetProvider(GitHubConfiguration configuration) 
            => this.configuration = configuration;

        public async Task InitializeAsync(SnippetProviderContext context)
        {
            if (configuration.UserName == null)
                return;

            var github = new GitHubClient(new ProductHeaderValue("SnippetMananger"));
            var repositories = await github.Repository.GetAllForUser(configuration.UserName);

            foreach (var repository in repositories)
            {
                context.Models.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name}",
                    text: repository.HtmlUrl
                ));
                context.Models.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name} - Issues",
                    text: repository.HtmlUrl + "/issues",
                    priority: SnippetPriority.Low
                ));
                context.Models.Add(new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login} - {repository.Name} - Issues - New",
                    text: repository.HtmlUrl + "/issues/new",
                    priority: SnippetPriority.Low
                ));
            }
        }

        public Task UpdateAsync(SnippetProviderContext context)
            => Task.CompletedTask;
    }
}
