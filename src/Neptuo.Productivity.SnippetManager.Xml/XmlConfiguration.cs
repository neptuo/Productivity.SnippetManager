namespace Neptuo.Productivity.SnippetManager
{
    public class XmlConfiguration : ProviderConfiguration, IEquatable<XmlConfiguration>, IProviderConfiguration<XmlConfiguration>
    {
        public string? FilePath { get; set; }

        public string GetFilePathOrDefault()
        {
            string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string filePath = FilePath ?? Path.Combine(userProfilePath, "SnippetManager.xml");
            return Path.GetFullPath(filePath, userProfilePath);
        }

        public bool Equals(XmlConfiguration? other) 
            => base.Equals(other) && FilePath == other.FilePath;

        public static new XmlConfiguration Example
        {
            get
            {
                var configuration = new XmlConfiguration();
                configuration.FilePath = configuration.GetFilePathOrDefault();
                return configuration;
            }
        }
    }
}