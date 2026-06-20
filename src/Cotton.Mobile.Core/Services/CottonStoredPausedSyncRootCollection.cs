namespace Cotton.Mobile.Services
{
    internal class CottonStoredPausedSyncRootCollection
    {
        public int SchemaVersion { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public List<Guid>? RootIds { get; set; }
    }
}
