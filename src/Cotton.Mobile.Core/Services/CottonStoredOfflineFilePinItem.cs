namespace Cotton.Mobile.Services
{
    internal class CottonStoredOfflineFilePinItem
    {
        public Guid FileId { get; set; }

        public string? FileName { get; set; }

        public DateTime PinnedAtUtc { get; set; }

        public DateTime RemoteUpdatedAtUtc { get; set; }

        public long? SizeBytes { get; set; }

        public string? ContentType { get; set; }
    }
}
