// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Collections.ObjectModel;

namespace Cotton.Mobile.Services
{
    public class CottonFileRevisionSnapshot(
        DateTime updatedAt,
        long? sizeBytes,
        string? contentType,
        string? contentHash,
        string? previewHashEncryptedHex,
        string? eTag,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        public DateTime UpdatedAt { get; } = updatedAt;

        public long? SizeBytes { get; } = sizeBytes;

        public string? ContentType { get; } = string.IsNullOrWhiteSpace(contentType)
            ? null
            : contentType.Trim();

        public string? ContentHash { get; } =
            CottonContentHash.NormalizeOptionalSha256(contentHash, nameof(contentHash));

        public string? PreviewHashEncryptedHex { get; } =
            string.IsNullOrWhiteSpace(previewHashEncryptedHex)
                ? null
                : previewHashEncryptedHex.Trim();

        public string? ETag { get; } = string.IsNullOrWhiteSpace(eTag) ? null : eTag.Trim();

        public IReadOnlyDictionary<string, string> Metadata { get; } = CreateMetadata(metadata);

        private static ReadOnlyDictionary<string, string> CreateMetadata(
            IReadOnlyDictionary<string, string>? metadata)
        {
            Dictionary<string, string> values = metadata is null
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : new Dictionary<string, string>(metadata, StringComparer.Ordinal);
            return new ReadOnlyDictionary<string, string>(values);
        }
    }
}
