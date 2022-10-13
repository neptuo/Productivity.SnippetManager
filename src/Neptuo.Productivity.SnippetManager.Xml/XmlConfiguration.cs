using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Productivity.SnippetManager
{
    public class XmlConfiguration : ProviderConfiguration
    {
        public string? FilePath { get; set; }

        public string GetFilePathOrDefault()
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = FilePath ?? Path.Combine(userProfilePath, "SnippetManager.xml");
            return Path.GetFullPath(filePath, userProfilePath);
        }
    }
}
