// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public class CottonFileVersionItemSnapshot
    {
        private const string UnknownText = "Unknown";
        private const string UntitledFileText = "Untitled file";

        private CottonFileVersionItemSnapshot(
            Guid versionId,
            Guid nodeFileId,
            Guid fileManifestId,
            string name,
            string kindText,
            string sizeText,
            string contentTypeText,
            string versionText,
            string createdText,
            string updatedText,
            string detailText,
            int versionNumber,
            bool isCurrent,
            bool isOriginal,
            bool canDelete)
        {
            if (versionId == Guid.Empty)
            {
                throw new ArgumentException("File version id is required.", nameof(versionId));
            }

            if (nodeFileId == Guid.Empty)
            {
                throw new ArgumentException("File version node id is required.", nameof(nodeFileId));
            }

            if (fileManifestId == Guid.Empty)
            {
                throw new ArgumentException("File version manifest id is required.", nameof(fileManifestId));
            }

            if (versionNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(versionNumber), "File version number must be positive.");
            }

            VersionId = versionId;
            NodeFileId = nodeFileId;
            FileManifestId = fileManifestId;
            Name = string.IsNullOrWhiteSpace(name) ? UntitledFileText : name.Trim();
            KindText = string.IsNullOrWhiteSpace(kindText) ? "File" : kindText.Trim();
            SizeText = string.IsNullOrWhiteSpace(sizeText) ? UnknownText : sizeText.Trim();
            ContentTypeText = string.IsNullOrWhiteSpace(contentTypeText) ? UnknownText : contentTypeText.Trim();
            VersionText = string.IsNullOrWhiteSpace(versionText) ? $"Version {versionNumber}" : versionText.Trim();
            CreatedText = string.IsNullOrWhiteSpace(createdText) ? UnknownText : createdText.Trim();
            UpdatedText = string.IsNullOrWhiteSpace(updatedText) ? UnknownText : updatedText.Trim();
            DetailText = string.IsNullOrWhiteSpace(detailText)
                ? $"{SizeText} · {KindText}"
                : detailText.Trim();
            VersionNumber = versionNumber;
            IsCurrent = isCurrent;
            IsOriginal = isOriginal;
            CanDelete = canDelete;
        }

        public Guid VersionId { get; }

        public Guid NodeFileId { get; }

        public Guid FileManifestId { get; }

        public string Name { get; }

        public string KindText { get; }

        public string SizeText { get; }

        public string ContentTypeText { get; }

        public string VersionText { get; }

        public string CreatedText { get; }

        public string UpdatedText { get; }

        public string DetailText { get; }

        public int VersionNumber { get; }

        public bool IsCurrent { get; }

        public bool IsOriginal { get; }

        public bool CanDelete { get; }

        public static CottonFileVersionItemSnapshot Create(
            FileVersionDto version,
            TimeZoneInfo displayTimeZone)
        {
            ArgumentNullException.ThrowIfNull(version);
            ArgumentNullException.ThrowIfNull(displayTimeZone);

            string name = string.IsNullOrWhiteSpace(version.Name)
                ? UntitledFileText
                : version.Name.Trim();
            string contentType = CottonFileKindClassifier.CreateContentTypeMediaType(version.ContentType);
            string contentTypeText = string.IsNullOrWhiteSpace(contentType) ? UnknownText : contentType;
            string kindText = CottonFileKindClassifier.ResolveKind(name, contentType);
            string sizeText = CottonFileSizeFormatter.Format(version.SizeBytes);
            string updatedText = FormatTimestamp(version.UpdatedAt, displayTimeZone);
            string createdText = FormatTimestamp(version.CreatedAt, displayTimeZone);
            string versionText = CreateVersionText(version);
            string detailText = string.Equals(updatedText, UnknownText, StringComparison.Ordinal)
                ? $"{sizeText} · {kindText}"
                : $"{sizeText} · {kindText} · Updated {updatedText}";

            return new CottonFileVersionItemSnapshot(
                version.Id,
                version.NodeFileId,
                version.FileManifestId,
                name,
                kindText,
                sizeText,
                contentTypeText,
                versionText,
                createdText,
                updatedText,
                detailText,
                version.VersionNumber,
                version.IsCurrent,
                version.IsOriginal,
                version.CanDelete);
        }

        private static string CreateVersionText(FileVersionDto version)
        {
            var parts = new List<string>();
            if (version.IsCurrent)
            {
                parts.Add("Current");
            }
            else
            {
                parts.Add($"Version {version.VersionNumber}");
            }

            if (version.IsOriginal)
            {
                parts.Add("Original");
            }

            return string.Join(" · ", parts);
        }

        private static string FormatTimestamp(DateTime value, TimeZoneInfo displayTimeZone)
        {
            if (value == default)
            {
                return UnknownText;
            }

            DateTime utc = CottonLocalFileFreshness.NormalizeUtc(value);
            DateTime displayTime = TimeZoneInfo.ConvertTimeFromUtc(utc, displayTimeZone);
            return $"{displayTime:yyyy-MM-dd HH:mm}";
        }
    }
}
