// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Mobile.Resources.Localization;
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
                CoreResources.FolderKind,
                CoreResources.OpenAction,
                CoreResources.FolderKind,
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
            string displayKind = CottonFileKindDisplayName.Create(kind);
            return new CottonFileBrowserEntry(
                file.Id,
                CottonFileBrowserEntryType.File,
                file.Name,
                kind,
                $"{CottonFileSizeFormatter.Format(file.SizeBytes)} · {displayKind}",
                CoreResources.MoreAction,
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
            string displayKind = CottonFileKindDisplayName.Create(kind);
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {displayKind}"
                : displayKind;
            return new CottonFileBrowserEntry(
                id,
                CottonFileBrowserEntryType.File,
                name,
                kind,
                details,
                CoreResources.MoreAction,
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
                "Image" => CoreResources.ImageBadge,
                "PDF" => "PDF",
                "Document" => CoreResources.DocumentBadge,
                "Video" => CoreResources.VideoBadge,
                "Audio" => CoreResources.AudioBadge,
                "SVG" => "SVG",
                "Text" => CoreResources.TextBadge,
                "File" => CoreResources.FileBadge,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "File kind is not supported."),
            };
        }
    }
}
