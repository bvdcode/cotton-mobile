// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;
using Cotton.Mobile.Resources.Localization;
using Cotton.Nodes;

namespace Cotton.Mobile.Services
{
    public static class CottonFileBrowserEntryFactory
    {
        public static CottonFileBrowserEntry FromNode(NodeDto node)
        {
            ArgumentNullException.ThrowIfNull(node);

            return CreateFolder(node.Id, node.Name, node.UpdatedAt);
        }

        public static CottonFileBrowserEntry CreateFolder(
            Guid id,
            string name,
            DateTime updatedAt)
        {
            CottonFileDescriptor descriptor = new(
                id,
                CottonFileBrowserEntryType.Folder,
                name,
                CottonFileKind.Folder);
            CottonFileRevisionSnapshot revision = new(
                updatedAt,
                sizeBytes: null,
                contentType: null,
                contentHash: null,
                previewHashEncryptedHex: null,
                eTag: null);
            CottonFileBrowserPresentation presentation = new(
                CoreResources.FolderKind,
                CoreResources.OpenAction,
                CoreResources.FolderKind);
            return new CottonFileBrowserEntry(descriptor, revision, presentation);
        }

        public static CottonFileBrowserEntry FromFile(NodeFileManifestDto file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return CreateFile(
                file.Id,
                file.Name,
                file.UpdatedAt,
                file.SizeBytes,
                file.ContentType,
                file.PreviewHashEncryptedHex,
                file.ETag,
                file.Metadata,
                file.ContentHash);
        }

        public static CottonFileBrowserEntry CreateFile(
            Guid id,
            string name,
            DateTime updatedAt,
            long? sizeBytes,
            string? contentType,
            string? previewHashEncryptedHex,
            string? eTag,
            IReadOnlyDictionary<string, string>? metadata = null,
            string? contentHash = null)
        {
            string normalizedContentType = string.IsNullOrWhiteSpace(contentType)
                ? string.Empty
                : contentType.Trim();
            CottonFileKind kind = CottonFileKindClassifier.ResolveKind(name, normalizedContentType);
            string displayKind = CottonFileKindDisplayName.Create(kind);
            string details = sizeBytes.HasValue
                ? $"{CottonFileSizeFormatter.Format(sizeBytes.Value)} · {displayKind}"
                : displayKind;
            CottonFileDescriptor descriptor = new(
                id,
                CottonFileBrowserEntryType.File,
                name,
                kind);
            CottonFileRevisionSnapshot revision = new(
                updatedAt,
                sizeBytes,
                normalizedContentType,
                contentHash,
                previewHashEncryptedHex,
                eTag,
                metadata);
            CottonFileBrowserPresentation presentation = new(
                details,
                CoreResources.MoreAction,
                ResolveBadgeText(kind));
            return new CottonFileBrowserEntry(descriptor, revision, presentation);
        }

        private static string ResolveBadgeText(CottonFileKind kind)
        {
            return kind switch
            {
                CottonFileKind.Image => CoreResources.ImageBadge,
                CottonFileKind.Pdf => "PDF",
                CottonFileKind.Document => CoreResources.DocumentBadge,
                CottonFileKind.Video => CoreResources.VideoBadge,
                CottonFileKind.Audio => CoreResources.AudioBadge,
                CottonFileKind.Svg => "SVG",
                CottonFileKind.Text => CoreResources.TextBadge,
                CottonFileKind.File => CoreResources.FileBadge,
                CottonFileKind.Folder => CoreResources.FolderKind,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(kind),
                    kind,
                    "File kind is not supported."),
            };
        }
    }
}
