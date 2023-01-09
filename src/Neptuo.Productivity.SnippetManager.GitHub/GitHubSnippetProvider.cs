using Neptuo.Collections.Generic;
using Neptuo.Observables.Collections;
using Neptuo.Productivity.SnippetManager.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                        Debug.WriteLine($"GitHub snippets for '{organization.Login}'");
                        var orgRepositories = await github.Repository.GetAllForOrg(organization.Login);
                        Debug.WriteLine($"GitHub snippets downloaded");
                        AddSnippetsForRepositories(context, orgRepositories);
                        Debug.WriteLine($"GitHub snippets added");
                    }
                    catch (ForbiddenException)
                    { }
                }

                if (configuration.ExtraRepositories != null)
                {
                    List<SnippetModel> snippets = new List<SnippetModel>();
                    foreach (var extraRepository in configuration.ExtraRepositories)
                    {
                        var names = extraRepository.Split("/");
                        if (names.Length == 1)
                            names = new[] { configuration.UserName, names[0] };

                        AddSnippetsForRepository(snippets, names[1], $"https://github.com/{names[0]}/{names[1]}", names[0], null);
                    }

                    context.AddRange(snippets);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Debug.WriteLine($"GitHub done");
        }

        private static void AddSnippetsForRepositories(SnippetProviderContext context, IReadOnlyList<Repository> repositories)
        {
            List<SnippetModel> snippets = new List<SnippetModel>();

            foreach (var repository in repositories)
            {
                AddSnippetsForRepository(
                    snippets, 
                    repository.Name,
                    repository.HtmlUrl,
                    repository.Owner.Login,
                    repository.DefaultBranch
                );
            }

            context.AddRange(snippets);
        }

        private static void AddSnippetsForRepository(ICollection<SnippetModel> snippets, string repository, string htmlUrl, string owner, string? defaultBranch)
        {
            snippets.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository}",
                text: htmlUrl
            ));
            snippets.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Issues",
                text: $"{htmlUrl}/issues",
                priority: SnippetPriority.Low
            ));
            snippets.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Issues - New",
                text: $"{htmlUrl}/issues/new",
                priority: SnippetPriority.Low
            ));
            snippets.Add(new SnippetModel(
                title: $"GitHub - {owner} - {repository} - Pull requests",
                text: $"{htmlUrl}/pulls",
                priority: SnippetPriority.Low
            ));

            if (defaultBranch != null)
            {
                snippets.Add(new SnippetModel(
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
