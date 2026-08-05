// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    internal static class CottonFileBrowserEntryFactory
    {
        public static CottonFileBrowserEntry FromNode(NodeDto node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return new CottonFileBrowserEntry(
                node.Id,
                CottonFileBrowserEntryType.Folder,
                node.Name,
                "Folder",
                "Folder",
                "Open",
                "Folder",
                node.UpdatedAt,
                sizeBytes: null,
                contentType: null,
                contentHash: null,
                previewHashEncryptedHex: null,
                eTag: null);
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            ArgumentNullException.ThrowIfNull(file);

            string contentType = string.IsNullOrWhiteSpace(file.ContentType)
                ? string.Empty
                : file.ContentType.Trim();
            string kind = CottonFileKindClassifier.ResolveKind(file.Name, contentType);
            return new CottonFileBrowserEntry(
                file.Id,
                CottonFileBrowserEntryType.File,
                file.Name,
                kind,
                $"{CottonFileSizeFormatter.Format(file.SizeBytes)} · {kind}",
                "More",
                ResolveBadgeText(kind),
                file.UpdatedAt,
                file.SizeBytes,
                contentType,
                file.ContentHash,
                file.PreviewHashEncryptedHex,
                file.ETag,
                metadata: file.Metadata);
        }

        public static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag,
            IReadOnlyDictionary<string, string>? metadata,
            string? contentHash)
        {
            string kind = CottonFileKindClassifier.ResolveKind(name, contentType);
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {kind}"
                : kind;
            return new CottonFileBrowserEntry(
                id,
                CottonFileBrowserEntryType.File,
                name,
                kind,
                details,
                "More",
                ResolveBadgeText(kind),
                updatedAtUtc,
                sizeBytes,
                contentType,
                contentHash,
                previewHashEncryptedHex,
                eTag,
                metadata: metadata);
        }

        public static CottonFileBrowserEntry CreateCached(
            Guid id,
            CottonFileBrowserEntryType type,
            string name,
            string kind,
            string details,
            string actionLabel,
            string badgeText,
            DateTime updatedAtUtc,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag,
            string? contentHash)
        {
            return new CottonFileBrowserEntry(
                id,
                type,
                name,
                kind,
                details,
                actionLabel,
                badgeText,
                updatedAtUtc,
                sizeBytes,
                contentType,
                contentHash,
                previewHashEncryptedHex,
                eTag);
        }

        private static string ResolveBadgeText(string kind)
        {
            return kind switch
            {
                "Image" => "IMG",
                "PDF" => "PDF",
                "Document" => "DOC",
                "Video" => "VID",
                "Audio" => "AUD",
                "SVG" => "SVG",
                "Text" => "TXT",
                _ => "FILE",
            };
        }
    }
}
