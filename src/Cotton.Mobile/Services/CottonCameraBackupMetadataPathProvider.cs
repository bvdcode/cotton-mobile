namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupMetadataPathProvider : ICottonCameraBackupMetadataPathProvider
    {
        public string CreateCameraBackupMetadataDirectory(Uri instanceUri)
        {
            return CottonMobileStoragePaths.CreateCameraBackupMetadataDirectory(instanceUri);
        }
    }
}
