namespace Cotton.Mobile.Services
{
    public class CottonSyncServerRevisionSnapshot
    {
        private CottonSyncServerRevisionSnapshot(
            CottonSyncServerRevisionStatus status,
            string? eTag,
            string summaryText,
            string detailsText)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Server revision status is not supported.");
            }

            Status = status;
            ETag = string.IsNullOrWhiteSpace(eTag) ? null : eTag.Trim();
            SummaryText = string.IsNullOrWhiteSpace(summaryText)
                ? throw new ArgumentException("Summary text is required.", nameof(summaryText))
                : summaryText;
            DetailsText = string.IsNullOrWhiteSpace(detailsText)
                ? throw new ArgumentException("Details text is required.", nameof(detailsText))
                : detailsText;
        }

        public CottonSyncServerRevisionStatus Status { get; }

        public string? ETag { get; }

        public string SummaryText { get; }

        public string DetailsText { get; }

        public bool HasServerRevisionToken => Status == CottonSyncServerRevisionStatus.FileETag;

        public bool SupportsExpectedETagMutation => Status == CottonSyncServerRevisionStatus.FileETag;

        public static CottonSyncServerRevisionSnapshot FileWithETag(string eTag)
        {
            if (string.IsNullOrWhiteSpace(eTag))
            {
                throw new ArgumentException("ETag is required.", nameof(eTag));
            }

            return new CottonSyncServerRevisionSnapshot(
                CottonSyncServerRevisionStatus.FileETag,
                eTag,
                "File ETag available",
                "File changes can use the server ETag as an expected precondition.");
        }

        public static CottonSyncServerRevisionSnapshot FileMissingETag()
        {
            return new CottonSyncServerRevisionSnapshot(
                CottonSyncServerRevisionStatus.FileMissingETag,
                eTag: null,
                "File ETag missing",
                "File changes need a fresh server listing before conflict-safe mutation.");
        }

        public static CottonSyncServerRevisionSnapshot FolderUnsupported()
        {
            return new CottonSyncServerRevisionSnapshot(
                CottonSyncServerRevisionStatus.FolderUnsupported,
                eTag: null,
                "Folder revision unavailable",
                "The current SDK does not expose folder ETags or expected-ETag folder mutations.");
        }
    }
}
