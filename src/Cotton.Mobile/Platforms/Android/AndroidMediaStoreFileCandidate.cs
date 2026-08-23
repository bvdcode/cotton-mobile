// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidMediaStoreFileCandidate(
        AndroidUri contentUri,
        string displayName,
        string relativePath,
        string localSourceId,
        DateTime updatedAtUtc,
        long? sizeBytes,
        string? contentType,
        long? revision)
    {
        public AndroidUri ContentUri { get; } =
            contentUri ?? throw new ArgumentNullException(nameof(contentUri));

        public string DisplayName { get; } = displayName;

        public string RelativePath { get; } = relativePath;

        public string LocalSourceId { get; } = localSourceId;

        public DateTime UpdatedAtUtc { get; } = updatedAtUtc;

        public long? SizeBytes { get; } = sizeBytes;

        public string? ContentType { get; } = contentType;

        public long? Revision { get; } = revision;
    }
}
#endif
