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

    internal class FixedSyncRootMetadataPathProvider(string directory)
        : ICottonSyncRootMetadataPathProvider
    {
        private readonly string _directory = directory;

        public string CreateSyncRootMetadataDirectory(Uri instanceUri)
        {
            return _directory;
        }
    }

    internal class FixedSyncedFileManifestPathProvider(string rootDirectory)
        : ICottonSyncedFileManifestPathProvider
    {
        private readonly string _rootDirectory = rootDirectory;

        public string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return TestFilePaths.CreateSyncedFileManifestDirectory(_rootDirectory, instanceUri, root);
        }
    }

    internal class ScopedUploadReceiptPathProvider(string directory) : ICottonUploadReceiptPathProvider
    {
        private readonly string _directory = directory;

        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(_directory, root.StableKey);
        }
    }

    internal class FixedUploadReceiptPathProvider(string directory) : ICottonUploadReceiptPathProvider
    {
        private readonly string _directory = directory;

        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return _directory;
        }
    }

    internal class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        private readonly DateTimeOffset _utcNow =
            new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }
}
