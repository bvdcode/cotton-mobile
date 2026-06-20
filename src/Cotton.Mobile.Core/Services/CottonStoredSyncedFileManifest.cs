namespace Cotton.Mobile.Services
{
    internal class CottonStoredSyncedFileManifest
    {
        public int SchemaVersion { get; set; }

        public string? SyncRootStableKey { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public List<CottonStoredSyncedFileItem>? Items { get; set; }
    }
}
