using Neptuo.Productivity.SnippetManager.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace Neptuo.Productivity.SnippetManager
{
    public class GitHubSnippetProvider : ISnippetProvider
    {
        private readonly GitHubConfiguration configuration;

        public GitHubSnippetProvider(GitHubConfiguration configuration)
            => this.configuration = configuration;

        public async Task InitializeAsync(SnippetProviderContext context)
        {
            try
            {
                if (configuration.UserName == null)
                    return;

                var github = new GitHubClient(new ProductHeaderValue("SnippetMananger"));
                if (configuration.AccessToken != null)
                    github.Credentials = new Credentials(configuration.AccessToken);

                var repositories = await github.Repository.GetAllForUser(configuration.UserName);

                AddSnippetsForRepositories(context, repositories);

                var organizations = await github.Organization.GetAllForUser(configuration.UserName);
                foreach (var organization in organizations)
                {
                    try
                    {

                        var orgRepositories = await github.Repository.GetAllForOrg(organization.Login);
                        AddSnippetsForRepositories(context, orgRepositories);
                    }
                    catch (ForbiddenException)
                    { }
                }

                if (configuration.ExtraRepositories != null)
                {
                    foreach (var extraRepository in configuration.ExtraRepositories)
                    {
                        var names = extraRepository.Split("/");
                        if (names.Length == 1)
                            names = new[] { configuration.UserName, names[0] };

                        AddSnippetsForRepository(context, names[1], $"https://github.com/{names[0]}/{names[1]}", names[0], null);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void AddSnippetsForRepositories(SnippetProviderContext context, IReadOnlyList<Repository> repositories)
        {
            foreach (var repository in repositories)
            {
                AddSnippetsForRepository(
                    context, 
                    repository.Name,
                    repository.HtmlUrl,
                    repository.Owner.Login,
                    repository.DefaultBranch
                );
            }
        }

        private static void AddSnippetsForRepository(SnippetProviderContext context, string repository, string htmlUrl, string owner, string? defaultBranch)
        {
            context.Models.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository}",
                text: htmlUrl
            ));
            context.Models.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Issues",
                text: $"{htmlUrl}/issues",
                priority: SnippetPriority.Low
            ));
            context.Models.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Issues - New",
                text: $"{htmlUrl}/issues/new",
                priority: SnippetPriority.Low
            ));
            context.Models.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Pull requests",
                text: $"{htmlUrl}/pulls",
                priority: SnippetPriority.Low
            ));

            if (defaultBranch != null)
            {
                context.Models.Add(new SnippetModel(
                    title: $"GitHub - {owner} - {repository} - Find file",
                    text: $"{htmlUrl}/find/{defaultBranch}",
                    priority: SnippetPriority.Low
                ));
            }
        }

        public Task UpdateAsync(SnippetProviderContext context)
            => Task.CompletedTask;
    }
}
