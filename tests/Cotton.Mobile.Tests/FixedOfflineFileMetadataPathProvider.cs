using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FixedOfflineFileMetadataPathProvider(string rootDirectory)
        : ICottonOfflineFileMetadataPathProvider
    {
        public string CreateOfflineFileMetadataDirectory(Uri instanceUri)
        {
            return Path.Combine(rootDirectory, instanceUri.Host);
        }
    }
}
