namespace Cotton.Mobile.Services
{
    public class CottonShareIntakeItemSnapshot
    {
        public CottonShareIntakeItemSnapshot(
            Guid id,
            CottonShareIntakeItemType type,
            string value,
            string? displayName,
            string? mimeType)
            : this(
                id,
                type,
                value,
                displayName,
                mimeType,
                stagedFileName: null,
                stagedPath: null,
                stagedSizeBytes: null,
                uploadDisplayName: null)
        {
        }

        public CottonShareIntakeItemSnapshot(
            Guid id,
            CottonShareIntakeItemType type,
            string value,
            string? displayName,
            string? mimeType,
            string? stagedFileName,
            string? stagedPath,
            long? stagedSizeBytes)
            : this(
                id,
                type,
                value,
                displayName,
                mimeType,
                stagedFileName,
                stagedPath,
                stagedSizeBytes,
                uploadDisplayName: null)
        {
        }

        public CottonShareIntakeItemSnapshot(
            Guid id,
            CottonShareIntakeItemType type,
            string value,
            string? displayName,
            string? mimeType,
            string? stagedFileName,
            string? stagedPath,
            long? stagedSizeBytes,
            string? uploadDisplayName)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Share intake item id cannot be empty.", nameof(id));
            }

            if (!Enum.IsDefined(type))
            {
                throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Share intake item value cannot be empty.", nameof(value));
            }

            Id = id;
            Type = type;
            Value = value.Trim();
            DisplayName = NormalizeOptional(displayName);
            MimeType = NormalizeOptional(mimeType);
            StagedFileName = NormalizeOptional(stagedFileName);
            StagedPath = NormalizeOptional(stagedPath);
            StagedSizeBytes = stagedSizeBytes;
            UploadDisplayName = NormalizeOptional(uploadDisplayName);

            if (StagedSizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stagedSizeBytes));
            }

            if ((StagedFileName is null) != (StagedPath is null)
                || (StagedPath is null) != (StagedSizeBytes is null))
            {
                throw new ArgumentException("Share intake staged file metadata must be complete.");
            }
        }

        public Guid Id { get; }

        public CottonShareIntakeItemType Type { get; }

        public string Value { get; }

        public string? DisplayName { get; }

        public string? MimeType { get; }

        public string? StagedFileName { get; }

        public string? StagedPath { get; }

        public long? StagedSizeBytes { get; }

        public string? UploadDisplayName { get; }

        public string EffectiveUploadDisplayName => UploadDisplayName
            ?? DisplayName
            ?? StagedFileName
            ?? "Shared file";

        public bool HasStagedContent => StagedPath is not null;

        public CottonShareIntakeItemSnapshot WithStagedContent(CottonShareStagedContentSnapshot stagedContent)
        {
            ArgumentNullException.ThrowIfNull(stagedContent);
            if (stagedContent.ItemId != Id)
            {
                throw new ArgumentException("Staged content item id must match the intake item id.", nameof(stagedContent));
            }

            return new CottonShareIntakeItemSnapshot(
                Id,
                Type,
                Value,
                DisplayName,
                MimeType,
                stagedContent.FileName,
                stagedContent.Path,
                stagedContent.SizeBytes,
                UploadDisplayName);
        }

        public CottonShareIntakeItemSnapshot WithUploadDisplayName(string uploadDisplayName)
        {
            if (!CottonShareUploadNameValidator.TryNormalize(
                    uploadDisplayName,
                    [],
                    out string normalizedName,
                    out string errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(uploadDisplayName));
            }

            return new CottonShareIntakeItemSnapshot(
                Id,
                Type,
                Value,
                DisplayName,
                MimeType,
                StagedFileName,
                StagedPath,
                StagedSizeBytes,
                normalizedName);
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
