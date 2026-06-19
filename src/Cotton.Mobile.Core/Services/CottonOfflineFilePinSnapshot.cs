namespace Cotton.Mobile.Services
{
    public class CottonOfflineFilePinSnapshot
    {
        public CottonOfflineFilePinSnapshot(
            Guid fileId,
            string fileName,
            DateTime pinnedAtUtc,
            DateTime remoteUpdatedAtUtc,
            long? sizeBytes,
            string? contentType)
        {
            if (fileId == Guid.Empty)
            {
                throw new ArgumentException("File id is required.", nameof(fileId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name is required.", nameof(fileName));
            }

            if (sizeBytes is < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size cannot be negative.");
            }

            FileId = fileId;
            FileName = fileName.Trim();
            PinnedAtUtc = pinnedAtUtc;
            RemoteUpdatedAtUtc = remoteUpdatedAtUtc;
            SizeBytes = sizeBytes;
            ContentType = string.IsNullOrWhiteSpace(contentType) ? null : contentType.Trim();
        }

        public Guid FileId { get; }

        public string FileName { get; }

        public DateTime PinnedAtUtc { get; }

        public DateTime RemoteUpdatedAtUtc { get; }

        public long? SizeBytes { get; }

        public string? ContentType { get; }

        public static CottonOfflineFilePinSnapshot Create(
            CottonFileBrowserEntry file,
            DateTime pinnedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(file);
            if (file.Type != CottonFileBrowserEntryType.File)
            {
                throw new ArgumentException("Offline pin metadata requires a file entry.", nameof(file));
            }

            return new CottonOfflineFilePinSnapshot(
                file.Id,
                file.Name,
                pinnedAtUtc,
                file.UpdatedAtUtc,
                file.SizeBytes,
                file.ContentType);
        }
    }
}
