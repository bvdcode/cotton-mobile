using System.Globalization;
using System.Text;

namespace Cotton.Mobile.Services
{
    public sealed class CottonTransferSourceSnapshot
    {
        public CottonTransferSourceSnapshot(
            CottonTransferSourceKind kind,
            string sourceId,
            DateTime? lastModifiedUtc,
            long? sizeBytes,
            DateTime? capturedAtUtc)
        {
            if (!Enum.IsDefined(kind) || kind == CottonTransferSourceKind.Unknown)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Transfer source kind is not supported.");
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Transfer source id is required.", nameof(sourceId));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Transfer source size cannot be negative.");
            }

            Kind = kind;
            SourceId = sourceId.Trim();
            LastModifiedUtc = NormalizeUtc(lastModifiedUtc);
            SizeBytes = sizeBytes;
            CapturedAtUtc = NormalizeUtc(capturedAtUtc);
        }

        public CottonTransferSourceKind Kind { get; }

        public string SourceId { get; }

        public DateTime? LastModifiedUtc { get; }

        public long? SizeBytes { get; }

        public DateTime? CapturedAtUtc { get; }

        public static CottonTransferSourceSnapshot CreateCameraBackup(CottonCameraBackupCandidate candidate)
        {
            ArgumentNullException.ThrowIfNull(candidate);

            return new CottonTransferSourceSnapshot(
                CottonTransferSourceKind.CameraBackup,
                candidate.Identity.SourceId,
                candidate.Identity.LastModifiedUtc,
                candidate.Identity.SizeBytes,
                candidate.CapturedAtUtc);
        }

        public static CottonTransferSourceSnapshot CreateShareInbox(
            Guid itemId,
            DateTime receivedAtUtc,
            long sizeBytes)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Share inbox item id cannot be empty.", nameof(itemId));
            }

            return new CottonTransferSourceSnapshot(
                CottonTransferSourceKind.ShareInbox,
                itemId.ToString("D"),
                lastModifiedUtc: null,
                sizeBytes,
                receivedAtUtc);
        }

        public static CottonTransferSourceSnapshot CreateSelectedMedia(
            CottonFileUploadSourceSnapshot source,
            DateTime selectedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(source);

            return new CottonTransferSourceSnapshot(
                CottonTransferSourceKind.SelectedMedia,
                CreateSelectedMediaSourceId(source),
                TryGetOriginalLastModifiedUtc(source),
                source.SizeBytes,
                selectedAtUtc);
        }

        public bool MatchesCameraBackupIdentity(CottonCameraBackupMediaIdentity identity)
        {
            ArgumentNullException.ThrowIfNull(identity);

            return Kind == CottonTransferSourceKind.CameraBackup
                && string.Equals(SourceId, identity.SourceId, StringComparison.Ordinal)
                && LastModifiedUtc == identity.LastModifiedUtc
                && SizeBytes == identity.SizeBytes;
        }

        private static string CreateSelectedMediaSourceId(CottonFileUploadSourceSnapshot source)
        {
            string sourceKind = source.Metadata.TryGetValue(CottonFileUploadMetadataKeys.Source, out string? kind)
                ? kind
                : string.Empty;
            string originalLastModifiedUtc = source.Metadata.TryGetValue(
                CottonFileUploadMetadataKeys.OriginalLastModifiedUtc,
                out string? value)
                ? value
                : string.Empty;
            string size = source.SizeBytes?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            string sourceMaterial = string.Join(
                "|",
                "selected-media",
                sourceKind,
                source.Name,
                source.ContentType,
                size,
                originalLastModifiedUtc);
            return CottonFileUploadHash.CreateSha256Hex(Encoding.UTF8.GetBytes(sourceMaterial));
        }

        private static DateTime? TryGetOriginalLastModifiedUtc(CottonFileUploadSourceSnapshot source)
        {
            if (!source.Metadata.TryGetValue(
                    CottonFileUploadMetadataKeys.OriginalLastModifiedUtc,
                    out string? value))
            {
                return null;
            }

            return DateTime.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out DateTime parsed)
                ? NormalizeUtc(parsed)
                : null;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (value is null)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
        }
    }
}
