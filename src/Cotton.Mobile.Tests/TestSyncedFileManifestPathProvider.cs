using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class TestSyncedFileManifestPathProvider(string directory)
        : ICottonSyncedFileManifestPathProvider
    {
        public string CreateSyncedFileManifestDirectory(
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            return Path.Combine(directory, "manifests", root.Id.ToString("N"));
        }
    }
}
