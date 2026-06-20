namespace Cotton.Mobile.Services
{
    public class CottonSyncRootMetadataPathProvider : ICottonSyncRootMetadataPathProvider
    {
        public string CreateSyncRootMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateSyncRootMetadataDirectory(instanceUri);
        }
    }
}
