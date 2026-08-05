using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FixedSyncRootMetadataPathProvider(string directory)
        : ICottonSyncRootMetadataPathProvider
    {
        public string CreateSyncRootMetadataDirectory(Uri instanceUri)
        {
            return directory;
        }
    }
}
