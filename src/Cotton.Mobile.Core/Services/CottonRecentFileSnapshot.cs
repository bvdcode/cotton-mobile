namespace Cotton.Mobile.Services
{
    public class CottonRecentFileSnapshot
    {
        public CottonRecentFileSnapshot(
            Guid fileId,
            string fileName,
            string kind,
            string badgeText,
            DateTime remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType,
            DateTime lastUsedAtUtc,
            CottonRecentFileActionKind lastAction)
        {
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("Recent file id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Recent file name is required.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(kind))
            {
                throw new ArgumentException("Recent file kind is required.", nameof(kind));
            }

            if (string.IsNullOrWhiteSpace(badgeText))
            {
                throw new ArgumentException("Recent file badge is required.", nameof(badgeText));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Recent file size cannot be negative.");
            }

            if (!Enum.IsDefined(lastAction))
            {
                throw new ArgumentOutOfRangeException(nameof(lastAction), "Recent file action is unknown.");
            }

            FileId = fileId;
            FileName = fileName.Trim();
            Kind = kind.Trim();
            BadgeText = badgeText.Trim();
            RemoteUpdatedAtUtc = remoteUpdatedAtUtc;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
            LastUsedAtUtc = lastUsedAtUtc;
            LastAction = lastAction;
        }

        public Guid FileId { get; }

        public string FileName { get; }

        public string Kind { get; }

        public string BadgeText { get; }

        public DateTime RemoteUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public DateTime LastUsedAtUtc { get; }

        public CottonRecentFileActionKind LastAction { get; }

        public static CottonRecentFileSnapshot Create(
            CottonFileBrowserEntry file,
            CottonRecentFileActionKind action,
            DateTime usedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Recent file metadata requires a file entry.", nameof(file));
            }

            return new CottonRecentFileSnapshot(
                file.Id,
                file.Name,
                file.Kind,
                file.BadgeText,
                file.UpdatedAtUtc,
                file.SizeBytes,
                file.ContentType,
                usedAtUtc,
                action);
        }
    }
}
