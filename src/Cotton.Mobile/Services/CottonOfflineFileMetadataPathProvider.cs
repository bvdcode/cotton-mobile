namespace Cotton.Mobile.Services
{
    public class CottonOfflineFileMetadataPathProvider : ICottonOfflineFileMetadataPathProvider
    {
        public string CreateOfflineFileMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateOfflineFileMetadataDirectory(instanceUri);
        }
    }
}
