namespace Cotton.Mobile.Services
{
    public interface ICottonOfflineFileMetadataPathProvider
    {
        string CreateOfflineFileMetadataDirectory(Uri instanceUri);
    }
}
