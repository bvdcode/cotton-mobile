using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class ScopedUploadReceiptPathProvider(string directory) : ICottonUploadReceiptPathProvider
    {
        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return Path.Combine(directory, root.StableKey);
        }
    }
}
