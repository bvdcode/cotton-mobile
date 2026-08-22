using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class FixedUploadReceiptPathProvider(string directory) : ICottonUploadReceiptPathProvider
    {
        public string CreateUploadReceiptDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return directory;
        }
    }
}
