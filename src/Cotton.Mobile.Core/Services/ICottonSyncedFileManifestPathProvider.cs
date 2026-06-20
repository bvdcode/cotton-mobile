namespace Cotton.Mobile.Services
{
    public interface ICottonSyncedFileManifestPathProvider
    {
        string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root);
    }
}
