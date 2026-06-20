namespace Cotton.Mobile.Services
{
    internal class CottonStoredSyncRootItem
    {
        public Guid Id { get; set; }

        public string? InstanceUri { get; set; }

        public string? AccountScopeKey { get; set; }

        public Guid CloudFolderId { get; set; }

        public string? CloudFolderName { get; set; }

        public string? CloudFolderPath { get; set; }

        public CottonSyncRootStorageKind LocalStorageKind { get; set; }

        public string? LocalRootKey { get; set; }

        public string? LocalRootDisplayName { get; set; }

        public CottonSyncRootPermissionStatus LocalPermissionStatus { get; set; }

        public CottonSyncDirection Direction { get; set; }

        public string? StableKey { get; set; }
    }
}
