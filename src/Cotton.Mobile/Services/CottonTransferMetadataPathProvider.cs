namespace Cotton.Mobile.Services
{
    public class CottonTransferMetadataPathProvider : ICottonTransferMetadataPathProvider
    {
        public string CreateTransferMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateTransferMetadataDirectory(instanceUri);
        }
    }
}
