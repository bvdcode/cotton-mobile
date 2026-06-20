namespace Cotton.Mobile.Services
{
    public class CottonSyncLocalRootSnapshot
    {
        public CottonSyncLocalRootSnapshot(
            CottonSyncRootStorageKind storageKind,
            string rootKey,
            string displayName,
            CottonSyncRootPermissionStatus permissionStatus)
        {
            if (!Enum.IsDefined(storageKind))
            {
                throw new ArgumentOutOfRangeException(nameof(storageKind), "Sync root storage kind is not supported.");
            }

            if (string.IsNullOrWhiteSpace(rootKey))
            {
                throw new ArgumentException("Sync root key is required.", nameof(rootKey));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Sync root display name is required.", nameof(displayName));
            }

            if (!Enum.IsDefined(permissionStatus))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(permissionStatus),
                    "Sync root permission status is not supported.");
            }

            if (storageKind == CottonSyncRootStorageKind.AppPrivateDirectory
                && permissionStatus == CottonSyncRootPermissionStatus.NeedsUserGrant)
            {
                throw new ArgumentException(
                    "App-private sync roots do not use a user folder grant.",
                    nameof(permissionStatus));
            }

            StorageKind = storageKind;
            RootKey = rootKey.Trim();
            DisplayName = displayName.Trim();
            PermissionStatus = permissionStatus;
        }

        public CottonSyncRootStorageKind StorageKind { get; }

        public string RootKey { get; }

        public string DisplayName { get; }

        public CottonSyncRootPermissionStatus PermissionStatus { get; }

        public bool UsesAppPrivateStorage => StorageKind == CottonSyncRootStorageKind.AppPrivateDirectory;

        public bool RequiresPersistedUserGrant => StorageKind == CottonSyncRootStorageKind.UserSelectedDocumentTree;

        public bool CanReadWrite => PermissionStatus == CottonSyncRootPermissionStatus.Available;

        public bool NeedsUserAction =>
            PermissionStatus is CottonSyncRootPermissionStatus.NeedsUserGrant
                or CottonSyncRootPermissionStatus.Revoked;
    }
}
