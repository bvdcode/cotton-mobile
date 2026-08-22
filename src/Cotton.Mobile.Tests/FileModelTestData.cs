using Cotton.Files;

namespace Cotton.Mobile.Tests
{
    internal static class FileModelTestData
    {
        public static DateTime UpdatedAt { get; } =
            new(2026, 6, 18, 12, 0, 0, DateTimeKind.Utc);

        public static NodeFileManifestDto CreateFile(
            string name,
            string contentType,
            long sizeBytes,
            string? previewHashEncryptedHex = null,
            string? eTag = null,
            IReadOnlyDictionary<string, string>? metadata = null)
        {
            return new NodeFileManifestDto
            {
                Id = Guid.NewGuid(),
                Name = name,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                PreviewHashEncryptedHex = previewHashEncryptedHex ?? string.Empty,
                ETag = eTag ?? string.Empty,
                UpdatedAt = UpdatedAt,
                Metadata = metadata is null
                    ? []
                    : new Dictionary<string, string>(metadata, StringComparer.Ordinal),
            };
        }
    }
}
