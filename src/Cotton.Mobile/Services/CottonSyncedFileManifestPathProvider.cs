namespace Cotton.Mobile.Services
{
    public class CottonSyncedFileManifestPathProvider : ICottonSyncedFileManifestPathProvider
    {
        public string CreateSyncedFileManifestDirectory(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            return CottonMobileStoragePaths.CreateSyncedFileManifestDirectory(instanceUri, root);
        }
    }
}
