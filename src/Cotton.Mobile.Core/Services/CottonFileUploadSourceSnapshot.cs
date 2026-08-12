// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFileUploadSourceSnapshot
    {
        public const string DefaultContentType = "application/octet-stream";
        public static string DefaultFileName => CoreResources.UploadedFileName;

        public CottonFileUploadSourceSnapshot(
            string? name,
            string? contentType,
            long? sizeBytes,
            IReadOnlyDictionary<string, string>? metadata = null,
            string? contentHash = null)
        {
            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Upload size cannot be negative.");
            }

            Name = NormalizeName(name);
            ContentType = NormalizeContentType(contentType);
            SizeBytes = sizeBytes;
            Metadata = NormalizeMetadata(metadata);
            ContentHash = CottonContentHash.NormalizeOptionalSha256(contentHash, nameof(contentHash));
        }

        public string Name { get; }

        public string ContentType { get; }

        public long? SizeBytes { get; }

        public IReadOnlyDictionary<string, string> Metadata { get; }

        public string? ContentHash { get; }

        public CottonFileUploadSourceSnapshot WithName(string? name)
        {
            return new CottonFileUploadSourceSnapshot(name, ContentType, SizeBytes, Metadata, ContentHash);
        }

        public CottonFileUploadSourceSnapshot WithMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            return new CottonFileUploadSourceSnapshot(Name, ContentType, SizeBytes, metadata, ContentHash);
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return DefaultFileName;
            }

            string pathSafeName = name.Trim()
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            string fileName = Path.GetFileName(pathSafeName);
            return string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName.Trim();
        }

        private static string NormalizeContentType(string? contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType.Trim();
        }

        private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
        {
            if (metadata is null || metadata.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            Dictionary<string, string> normalized = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> item in metadata)
            {
                if (string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Value))
                {
                    continue;
                }

                normalized[item.Key.Trim()] = item.Value.Trim();
            }

            return normalized;
        }
    }
}
