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

namespace Neptuo.Productivity.SnippetManager;

public class GitHubSnippetProvider : SingleInitializeSnippetProvider
{
    private readonly GitHubConfiguration configuration;

    public GitHubSnippetProvider(GitHubConfiguration configuration)
        => this.configuration = configuration;

    protected override async Task InitializeOnceAsync(SnippetProviderContext context)
    {
        try
        {
            if (configuration.UserName == null)
                return;

            var parent = new SnippetModel(
                title: $"GitHub",
                text: "https://github.com"
            );
            context.Add(parent);

            var github = new GitHubClient(new ProductHeaderValue("SnippetMananger"));
            if (configuration.AccessToken != null)
                github.Credentials = new Credentials(configuration.AccessToken);

            var repositories = await github.Repository.GetAllForUser(configuration.UserName);

            AddSnippetsForRepositories(context, repositories);

            var organizations = await github.Organization.GetAllForUser(configuration.UserName);
            var organizationTasks = new Task[organizations.Count];
            for (int i = 0; i < organizations.Count; i++)
            {
                var organization = organizations[i];
                organizationTasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"GitHub snippets for '{organization.Login}'");
                        var orgRepositories = await github.Repository.GetAllForOrg(organization.Login);
                        Debug.WriteLine($"GitHub snippets downloaded for '{organization.Login}'");
                        AddSnippetsForRepositories(context, orgRepositories);
                        Debug.WriteLine($"GitHub snippets added for '{organization.Login}'");
                    }
                    catch (ForbiddenException)
                    {
                        Debug.WriteLine($"GitHub forbidden acces to organization '{organization.Login}'");
                    }
                });
            }

            await Task.WhenAll(organizationTasks);

            if (configuration.ExtraRepositories != null)
            {
                List<SnippetModel> snippets = new List<SnippetModel>();
                foreach (var extraRepository in configuration.ExtraRepositories)
                {
                    var names = extraRepository.Split("/");
                    if (names.Length == 1)
                        names = new[] { configuration.UserName, names[0] };

                    AddSnippetsForRepository(snippets, names[1], $"https://github.com/{names[0]}/{names[1]}", names[0], true, null);
                }

                context.AddRange(snippets);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GitHub exception: {ex.ToString()}");
        }

        Debug.WriteLine($"GitHub done");
    }

    private static void AddSnippetsForRepositories(SnippetProviderContext context, IReadOnlyList<Repository> repositories)
    {
        List<SnippetModel> snippets = new List<SnippetModel>();

        SnippetModel? parent = null;
        foreach (var repository in repositories)
        {
            if (snippets.Count == 0)
            {
                parent = new SnippetModel(
                    title: $"GitHub - {repository.Owner.Login}",
                    text: repository.Owner.HtmlUrl
                );
                context.Add(parent);
            }

            AddSnippetsForRepository(
                snippets,
                repository.Name,
                repository.HtmlUrl,
                repository.Owner.Login,
                repository.HasIssues,
                repository.DefaultBranch
            );
        }

        context.AddRange(snippets);
    }

    private static void AddSnippetsForRepository(ICollection<SnippetModel> snippets, string repository, string htmlUrl, string owner, bool hasIssues, string? defaultBranch)
    {
        var repositoryTitle = $"GitHub - {owner} - {repository}";

        snippets.Add(new SnippetModel(
            title: repositoryTitle,
            text: htmlUrl
        ));

        if (hasIssues)
        {
            snippets.Add(new SnippetModel(
                title: $"{repositoryTitle} - Issues",
                text: $"{htmlUrl}/issues",
                priority: SnippetPriority.Low
            ));
            snippets.Add(new SnippetModel(
                title: $"{repositoryTitle} - Issues - New",
                text: $"{htmlUrl}/issues/new",
                priority: SnippetPriority.Low
            ));
        }

        snippets.Add(new SnippetModel(
            title: $"{repositoryTitle} - Pulls",
            text: $"{htmlUrl}/pulls",
            priority: SnippetPriority.Low
        ));

        if (defaultBranch != null)
        {
            snippets.Add(new SnippetModel(
                title: $"{repositoryTitle} - Find file",
                text: $"{htmlUrl}/find/{defaultBranch}",
                priority: SnippetPriority.Low
            ));
        }

        snippets.Add(new SnippetModel(
            title: $"{repositoryTitle} - Code search",
            text: $"https://github.com/search?q=repo:{owner}/{repository}&type=code",
            priority: SnippetPriority.Low
        ));
    }
}
