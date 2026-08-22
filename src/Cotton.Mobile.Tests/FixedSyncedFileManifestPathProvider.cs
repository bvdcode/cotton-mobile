using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FixedSyncedFileManifestPathProvider(string rootDirectory)
        : ICottonSyncedFileManifestPathProvider
    {
        public string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return TestFilePaths.CreateSyncedFileManifestDirectory(rootDirectory, instanceUri, root);
        }
    }
}
