namespace Cotton.Mobile.Services
{
    public class CottonRecentFileMetadataPathProvider : ICottonRecentFileMetadataPathProvider
    {
        public string CreateRecentFileMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateRecentFileMetadataDirectory(instanceUri);
        }
    }
}
