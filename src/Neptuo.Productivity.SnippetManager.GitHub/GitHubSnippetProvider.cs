using Neptuo.Productivity.SnippetManager.Models;
using Octokit;
using System.Diagnostics;

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

            await Task.WhenAll(
                AddUserRepositoriesAsync(context, github),
                AddOrganizationsRepositoriesAsync(context, github),
                AddStarredRepositoriesAsync(context, github)
            );

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

    private async Task AddStarredRepositoriesAsync(SnippetProviderContext context, GitHubClient github)
    {
        if (configuration.IncludeStars)
        {
            var starredRepositories = await github.Activity.Starring.GetAllForUser(configuration.UserName);
            if (starredRepositories.Count > 0)
            {
                // Create a separate section for starred repos
                var starredParent = new SnippetModel(
                    title: $"GitHub - Stars",
                    text: $"https://github.com/{configuration.UserName}?tab=stars"
                );
                context.Add(starredParent);

                // Add snippets for starred repositories
                AddSnippetsForRepositories(context, starredRepositories, starredParent.Title);
            }
        }
    }

    private async Task AddOrganizationsRepositoriesAsync(SnippetProviderContext context, GitHubClient github)
    {
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
    }

    private async Task AddUserRepositoriesAsync(SnippetProviderContext context, GitHubClient github)
    {
        var repositories = await github.Repository.GetAllForUser(configuration.UserName);
        AddSnippetsForRepositories(context, repositories);
    }

    private void AddSnippetsForRepositories(SnippetProviderContext context, IReadOnlyList<Repository> repositories, string? title = null)
    {
        List<SnippetModel> snippets = new List<SnippetModel>();

        SnippetModel? parent = null;
        foreach (var repository in repositories)
        {
            if (title == null && snippets.Count == 0)
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
                repository.DefaultBranch,
                title
            );
        }

        context.AddRange(snippets);
    }

    private void AddSnippetsForRepository(ICollection<SnippetModel> snippets, string repository, string htmlUrl, string owner, bool hasIssues, string? defaultBranch, string? title = null)
    {
        var repositoryTitle = title != null 
            ? $"{title} - {repository}"
            : $"GitHub - {owner} - {repository}";

        var priority = configuration.HighPriorityRepositories?.Contains($"{owner}/{repository}") ?? false
            ? SnippetPriority.High
            : SnippetPriority.Normal;

        snippets.Add(new SnippetModel(
            title: repositoryTitle,
            text: htmlUrl,
            priority: priority
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

        snippets.Add(new SnippetModel(
            title: $"{repositoryTitle} - My activity",
            text: $"{htmlUrl}/activity?actor={configuration.UserName}",
            priority: SnippetPriority.Low
        ));
    }
}
