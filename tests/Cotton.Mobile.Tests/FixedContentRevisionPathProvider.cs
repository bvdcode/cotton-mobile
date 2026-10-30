using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FixedContentRevisionPathProvider(string directory) : ICottonContentRevisionPathProvider
    {
        private readonly string _directory = directory;

        public string CreateContentRevisionFilePath(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(_directory, root.StableKey, "content-revisions.json");
        }
    }
}
