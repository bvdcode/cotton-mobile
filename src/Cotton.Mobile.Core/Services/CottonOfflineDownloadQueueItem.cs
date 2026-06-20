namespace Cotton.Mobile.Services
{
    public class CottonOfflineDownloadQueueItem
    {
        public CottonOfflineDownloadQueueItem(
            int position,
            Guid fileId,
            string fileName,
            long sizeBytes,
            DateTime remoteUpdatedAtUtc,
            string? contentType)
            : this(position, fileId, fileName, fileName, sizeBytes, remoteUpdatedAtUtc, contentType)
        {
        }

        public CottonOfflineDownloadQueueItem(
            int position,
            Guid fileId,
            string fileName,
            string displayName,
            long sizeBytes,
            DateTime remoteUpdatedAtUtc,
            string? contentType)
        {
            if (position <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), "Position must be positive.");
            }

            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Display name is required.", nameof(displayName));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size cannot be negative.");
            }

            Position = position;
            FileId = fileId;
            FileName = fileName.Trim();
            DisplayName = displayName.Trim();
            SizeBytes = sizeBytes;
            RemoteUpdatedAtUtc = remoteUpdatedAtUtc;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public int Position { get; }

        public Guid FileId { get; }

        public string FileName { get; }

        public string DisplayName { get; }

        public long SizeBytes { get; }

        public DateTime RemoteUpdatedAtUtc { get; }

        public string? ContentType { get; }

        public CottonOfflineFilePinSnapshot CreatePin(DateTime pinnedAtUtc)
        {
            return new CottonOfflineFilePinSnapshot(
                FileId,
                FileName,
                pinnedAtUtc,
                RemoteUpdatedAtUtc,
                SizeBytes,
                ContentType);
        }

        public static CottonOfflineDownloadQueueItem Create(int position, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return Create(position, file, file.Name);
        }

        public static CottonOfflineDownloadQueueItem Create(
            int position,
            CottonFileBrowserEntry file,
            string displayName)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Offline download queue items require file entries.", nameof(file));
            }

            if (!file.SizeBytes.HasValue)
            {
                throw new ArgumentException("Offline download queue items require known file sizes.", nameof(file));
            }

            return new CottonOfflineDownloadQueueItem(
                position,
                file.Id,
                file.Name,
                displayName,
                file.SizeBytes.Value,
                file.UpdatedAtUtc,
                file.ContentType);
        }
    }
}
