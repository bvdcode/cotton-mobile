using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal static class SyncedFileManifestTestData
    {
        public static CottonSyncRootSnapshot CreateManifestRoot(
            Uri instanceUri,
            Guid rootId,
            Guid folderId,
            string localRootKey)
        {
            return new CottonSyncRootSnapshot(
                rootId,
                instanceUri,
                "account-1",
                new CottonUploadDestinationSnapshot(
                    folderId,
                    "Projects",
                    "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    localRootKey,
                    "On this device",
                    CottonSyncRootPermissionStatus.Available),
                CottonSyncDirection.CloudToDevice,
                CottonUploadOriginalRetention.KeepOriginals);
        }
    }
}
