// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Services
{
    public class CottonFileBrowserEntry
    {
        private readonly CottonFileDescriptor _descriptor;
        private readonly CottonFileRevisionSnapshot _revision;
        private readonly CottonFileBrowserPresentation _presentation;

        internal CottonFileBrowserEntry(
            CottonFileDescriptor descriptor,
            CottonFileRevisionSnapshot revision,
            CottonFileBrowserPresentation presentation,
            CottonOfflineFileAvailabilitySnapshot? offlineAvailability = null,
            CottonLocalFileSnapshot? localFile = null,
            CottonFileThumbnailSnapshot? thumbnail = null,
            bool isSelected = false)
        {
            _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _revision = revision ?? throw new ArgumentNullException(nameof(revision));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            OfflineAvailability = offlineAvailability ?? CottonOfflineFileAvailabilitySnapshot.NotPinned;
            LocalFile = localFile;
            Thumbnail = thumbnail ?? CottonFileThumbnailSnapshot.Placeholder(
                BadgeText,
                CreatePlaceholderThumbnailCacheKey());
            IsSelected = isSelected;
        }

        public Guid Id => _descriptor.Id;

        public CottonFileBrowserEntryType Type => _descriptor.Type;

        public string Name => _descriptor.Name;

        public CottonFileKind Kind => _descriptor.Kind;

        public string Details => _presentation.Details;

        public string DisplayDetails
        {
            get
            {
                if (LocalFile is not null)
                {
                    return $"{Details} · {CoreResources.OnDeviceStatus}";
                }

                return IsOfflineAttentionVisible
                    ? $"{Details} · {OfflineAvailability.StatusText}"
                    : Details;
            }
        }

        public bool HasLocalCopy => LocalFile is not null;

        public string LocalCopyStatus => HasLocalCopy ? CoreResources.OnDeviceStatus : string.Empty;

        public CottonOfflineFileAvailabilitySnapshot OfflineAvailability { get; }

        public bool IsOfflineAttentionVisible => LocalFile is null && OfflineAvailability.IsAttentionVisible;

        public string OfflineAttentionStatus =>
            IsOfflineAttentionVisible ? OfflineAvailability.StatusText : string.Empty;

        public string ActionLabel => _presentation.ActionLabel;

        public string BadgeText => _presentation.BadgeText;

        public DateTime UpdatedAtUtc => _revision.UpdatedAt;

        public long? SizeBytes => _revision.SizeBytes;

        public string? ContentType => _revision.ContentType;

        public string? ContentHash => _revision.ContentHash;

        public string? PreviewHashEncryptedHex => _revision.PreviewHashEncryptedHex;

        public string? ETag => _revision.ETag;

        public IReadOnlyDictionary<string, string> Metadata => _revision.Metadata;

        public CottonLocalFileSnapshot? LocalFile { get; }

        public CottonFileThumbnailSnapshot Thumbnail { get; }

        public bool IsSelected { get; }

        public bool IsFolder => Type == CottonFileBrowserEntryType.Folder;

        public bool IsFolderThumbnailVisible => IsFolder && Thumbnail.IsPlaceholderVisible;

        public bool IsPlaceholderTextVisible =>
            !IsFolder && (Thumbnail.IsPlaceholderVisible || (IsText && Thumbnail.HasImage));

        public bool IsPreviewImageVisible => Thumbnail.HasImage && !IsText;

        public bool IsImage => Type == CottonFileBrowserEntryType.File && Kind == CottonFileKind.Image;

        public bool IsText => Type == CottonFileBrowserEntryType.File && Kind == CottonFileKind.Text;

        public bool IsSvg => Type == CottonFileBrowserEntryType.File && Kind == CottonFileKind.Svg;

        public bool Matches(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return true;
            }

            string query = searchText.Trim();
            return Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || CottonFileKindDisplayName.Create(Kind).Contains(query, StringComparison.OrdinalIgnoreCase)
                || Details.Contains(query, StringComparison.OrdinalIgnoreCase)
                || (ContentType?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
        }

        public CottonFileBrowserEntry WithThumbnail(CottonFileThumbnailSnapshot thumbnail)
        {
            ArgumentNullException.ThrowIfNull(thumbnail);
            return Copy(OfflineAvailability, LocalFile, thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithLocalFile(CottonLocalFileSnapshot localFile)
        {
            ArgumentNullException.ThrowIfNull(localFile);
            return Copy(OfflineAvailability, localFile, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithOfflineAvailability(
            CottonOfflineFileAvailabilitySnapshot offlineAvailability)
        {
            ArgumentNullException.ThrowIfNull(offlineAvailability);
            return Copy(offlineAvailability, LocalFile, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithoutLocalFile()
        {
            return LocalFile is null
                ? this
                : Copy(OfflineAvailability, null, Thumbnail, IsSelected);
        }

        public CottonFileBrowserEntry WithSelection(bool isSelected)
        {
            return IsSelected == isSelected
                ? this
                : Copy(OfflineAvailability, LocalFile, Thumbnail, isSelected);
        }

        private CottonFileBrowserEntry Copy(
            CottonOfflineFileAvailabilitySnapshot offlineAvailability,
            CottonLocalFileSnapshot? localFile,
            CottonFileThumbnailSnapshot thumbnail,
            bool isSelected)
        {
            return new CottonFileBrowserEntry(
                _descriptor,
                _revision,
                _presentation,
                offlineAvailability,
                localFile,
                thumbnail,
                isSelected);
        }

        private string CreatePlaceholderThumbnailCacheKey()
        {
            return $"{Type}:{Id:N}:placeholder";
        }
    }
}
