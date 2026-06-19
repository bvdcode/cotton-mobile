namespace Cotton.Mobile.Services
{
    public class CottonOfflineFileAvailabilitySnapshot
    {
        private CottonOfflineFileAvailabilitySnapshot(CottonOfflineFileAvailabilityStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Offline file availability status is unknown.");
            }

            Status = status;
            StatusText = CreateStatusText(status);
            DetailsText = CreateDetailsText(status);
        }

        public static CottonOfflineFileAvailabilitySnapshot NotPinned { get; } =
            new(CottonOfflineFileAvailabilityStatus.NotPinned);

        public CottonOfflineFileAvailabilityStatus Status { get; }

        public bool IsPinned => Status != CottonOfflineFileAvailabilityStatus.NotPinned;

        public bool IsAvailable => Status == CottonOfflineFileAvailabilityStatus.Available;

        public bool NeedsRefresh =>
            Status == CottonOfflineFileAvailabilityStatus.Missing
            || Status == CottonOfflineFileAvailabilityStatus.Stale;

        public bool IsAttentionVisible => NeedsRefresh;

        public string StatusText { get; }

        public string DetailsText { get; }

        public static CottonOfflineFileAvailabilitySnapshot Create(
            CottonFileBrowserEntry file,
            CottonOfflineFilePinSnapshot? pin,
            CottonLocalFileSnapshot? localFile)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (pin is null)
            {
                return NotPinned;
            }

            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Offline file availability requires a file entry.", nameof(file));
            }

            if (pin.FileId != file.Id)
            {
                throw new ArgumentException("Offline file pin must match the file entry.", nameof(pin));
            }

            if (localFile is null)
            {
                return new CottonOfflineFileAvailabilitySnapshot(CottonOfflineFileAvailabilityStatus.Missing);
            }

            long? expectedSize = file.SizeBytes ?? pin.SizeBytes;
            if (expectedSize.HasValue && expectedSize.Value != localFile.SizeBytes)
            {
                return new CottonOfflineFileAvailabilitySnapshot(CottonOfflineFileAvailabilityStatus.Stale);
            }

            return CottonLocalFileFreshness.IsFresh(localFile.UpdatedAtUtc, file.UpdatedAtUtc)
                ? new CottonOfflineFileAvailabilitySnapshot(CottonOfflineFileAvailabilityStatus.Available)
                : new CottonOfflineFileAvailabilitySnapshot(CottonOfflineFileAvailabilityStatus.Stale);
        }

        private static string CreateStatusText(CottonOfflineFileAvailabilityStatus status)
        {
            return status switch
            {
                CottonOfflineFileAvailabilityStatus.Available => "On device",
                CottonOfflineFileAvailabilityStatus.Missing => "Offline missing",
                CottonOfflineFileAvailabilityStatus.Stale => "Offline stale",
                _ => string.Empty,
            };
        }

        private static string CreateDetailsText(CottonOfflineFileAvailabilityStatus status)
        {
            return status switch
            {
                CottonOfflineFileAvailabilityStatus.Available => "Available offline.",
                CottonOfflineFileAvailabilityStatus.Missing => "Kept offline, but missing on this device.",
                CottonOfflineFileAvailabilityStatus.Stale => "Kept offline, refresh to match the cloud version.",
                _ => string.Empty,
            };
        }
    }
}
