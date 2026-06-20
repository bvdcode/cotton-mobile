namespace Cotton.Mobile.Services
{
    public class CottonCachedFileBrowserEntry
    {
        public Guid Id { get; set; }

        public CottonFileBrowserEntryType Type { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public string Details { get; set; } = string.Empty;

        public string ActionLabel { get; set; } = string.Empty;

        public string BadgeText { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; }

        public long? SizeBytes { get; set; }

        public string? ContentType { get; set; }

        public string? PreviewHashEncryptedHex { get; set; }

        public string? ETag { get; set; }
    }
}
