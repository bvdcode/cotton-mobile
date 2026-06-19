namespace Cotton.Mobile.Services
{
    public interface ICottonCameraBackupMetadataPathProvider
    {
        string CreateCameraBackupMetadataDirectory(Uri instanceUri);
    }
}
