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

            AddSnippetsForRepositories(context, repositories, parent.Id);

#if !DEBUG
    throw new Exception("This was committed accidentallly!");
#endif
            //var organizations = await github.Organization.GetAllForUser(configuration.UserName);
            //foreach (var organization in organizations)
            //{
            //    try
            //    {
            //        Debug.WriteLine($"GitHub snippets for '{organization.Login}'");
            //        var orgRepositories = await github.Repository.GetAllForOrg(organization.Login);
            //        Debug.WriteLine($"GitHub snippets downloaded");
            //        AddSnippetsForRepositories(context, orgRepositories);
            //        Debug.WriteLine($"GitHub snippets added");
            //    }
            //    catch (ForbiddenException)
            //    { }
            //}

            //if (configuration.ExtraRepositories != null)
            //{
            //    List<SnippetModel> snippets = new List<SnippetModel>();
            //    foreach (var extraRepository in configuration.ExtraRepositories)
            //    {
            //        var names = extraRepository.Split("/");
            //        if (names.Length == 1)
            //            names = new[] { configuration.UserName, names[0] };

            //        AddSnippetsForRepository(snippets, names[1], $"https://github.com/{names[0]}/{names[1]}", names[0], true, null);
            //    }

            //    context.AddRange(snippets);
            //}
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        Debug.WriteLine($"GitHub done");
    }

    private static void AddSnippetsForRepositories(SnippetProviderContext context, IReadOnlyList<Repository> repositories, Guid parentId)
    {
        List<SnippetModel> snippets = new List<SnippetModel>();

        SnippetModel? parent = null;
        foreach (var repository in repositories)
        {
            if (snippets.Count == 0)
            {
                parent = new SnippetModel(
                    title: repository.Owner.Login,
                    text: repository.Owner.HtmlUrl,
                    parentId: parentId
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
                parent!.Id
            );
        }

        context.AddRange(snippets);
    }

    private static void AddSnippetsForRepository(ICollection<SnippetModel> snippets, string repository, string htmlUrl, string owner, bool hasIssues, string? defaultBranch, Guid parentId)
    {
        var parent = new SnippetModel(
            title: repository,
            text: htmlUrl,
            parentId: parentId
        );
        snippets.Add(parent);

        if (hasIssues)
        {
            snippets.Add(new SnippetModel(
                title: $"Issues",
                text: $"{htmlUrl}/issues",
                priority: SnippetPriority.Low,
                parentId: parent.Id
            ));
            snippets.Add(new SnippetModel(
                title: $"Issues - New",
                text: $"{htmlUrl}/issues/new",
                priority: SnippetPriority.Low,
                parentId: parent.Id
            ));
        }

        snippets.Add(new SnippetModel(
            title: $"Pulls",
            text: $"{htmlUrl}/pulls",
            priority: SnippetPriority.Low,
            parentId: parent.Id
        ));

        if (defaultBranch != null)
        {
            snippets.Add(new SnippetModel(
                title: $"Find file",
                text: $"{htmlUrl}/find/{defaultBranch}",
                priority: SnippetPriority.Low,
                parentId: parent.Id
            ));
        }
    }
}
