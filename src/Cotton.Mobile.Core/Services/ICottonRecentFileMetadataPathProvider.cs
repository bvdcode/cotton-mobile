namespace Cotton.Mobile.Services
{
    public interface ICottonRecentFileMetadataPathProvider
    {
        string CreateRecentFileMetadataDirectory(Uri instanceUri);
    }
}
