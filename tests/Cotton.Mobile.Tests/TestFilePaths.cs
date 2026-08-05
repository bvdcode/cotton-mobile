using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class TestFilePaths
    {
        public static string CreateSyncRootMetadataPath(string directory)
        {
            return Path.Combine(directory, FileSystemCottonSyncRootStore.MetadataFileName);
        }

        public static string CreateSyncedFileManifestDirectory(
            string rootDirectory,
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            return Path.Combine(rootDirectory, instanceUri.Host, root.StableKey);
        }

        public static string CreateSyncedFileManifestPath(
            string rootDirectory,
            Uri instanceUri,
            CottonSyncRootSnapshot root)
        {
            return Path.Combine(
                CreateSyncedFileManifestDirectory(rootDirectory, instanceUri, root),
                FileSystemCottonSyncedFileManifestStore.MetadataFileName);
        }
    }
}
