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

        public static new GitHubConfiguration Example => new()
        {
            UserName = "jon",
            AccessToken = "doe",
            ExtraRepositories = new List<string>(0)
        };

        public bool Equals(GitHubConfiguration? other)
        {
            return base.Equals(other) && UserName == other.UserName && AccessToken == other.AccessToken && EqualsExtraRepositories(other);
        }

        private bool EqualsExtraRepositories(GitHubConfiguration other)
        {
            if (ExtraRepositories != null && other.ExtraRepositories != null)
                return !ExtraRepositories.Except(other.ExtraRepositories).Any() && !other.ExtraRepositories.Except(ExtraRepositories).Any();

            if (ExtraRepositories == null && other.ExtraRepositories == null)
                return true;

            return false;
        }
    }
}
