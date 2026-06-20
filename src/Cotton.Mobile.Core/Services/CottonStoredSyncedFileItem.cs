namespace Cotton.Mobile.Services
{
    internal class CottonStoredSyncedFileItem
    {
        public Guid FileId { get; set; }

        public string? FileName { get; set; }

        public string? ETag { get; set; }

        public DateTime RemoteUpdatedAtUtc { get; set; }

        public long? SizeBytes { get; set; }

        public string? ContentType { get; set; }

        public DateTime SyncedAtUtc { get; set; }
    }
}
