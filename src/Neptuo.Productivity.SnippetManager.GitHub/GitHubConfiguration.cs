using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class GitHubConfiguration : ProviderConfiguration, IEquatable<GitHubConfiguration>, IProviderConfiguration<GitHubConfiguration>
    {
        public string? UserName { get; set; }
        public string? AccessToken { get; set; }
        public List<string>? ExtraRepositories { get; set; }
        public List<string>? HighPriorityRepositories { get; set; }
        public bool IncludeStars { get; set; } = true;

        public static new GitHubConfiguration Example => new()
        {
            UserName = "jon",
            AccessToken = "doe",
            ExtraRepositories = new List<string>(0)
        };

        public bool Equals(GitHubConfiguration? other) => base.Equals(other) 
            && UserName == other.UserName 
            && AccessToken == other.AccessToken 
            && IncludeStars == other.IncludeStars
            && ListEquals(ExtraRepositories, other.ExtraRepositories)
            && ListEquals(HighPriorityRepositories, other.HighPriorityRepositories);

        private static bool ListEquals(List<string>? first, List<string>? second)
        {
            if (first != null && second != null)
                return !first.Except(second).Any() && !second.Except(first).Any();

            if (first == null && second == null)
                return true;

            return false;
        }
    }
}
