namespace Cotton.Mobile.Services
{
    public interface ICottonSyncRootMetadataPathProvider
    {
        string CreateSyncRootMetadataDirectory(Uri instanceUri);
    }
}
