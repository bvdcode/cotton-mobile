// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeChild(AndroidUri uri, string documentId, string displayName, string mimeType)
    {
        public AndroidUri Uri { get; } = uri;

        public string DocumentId { get; } = documentId;

        public string DisplayName { get; } = displayName;

        public string MimeType { get; } = mimeType;

        public bool IsDirectory => string.Equals(
            MimeType,
            DocumentsContract.Document.MimeTypeDir,
            StringComparison.Ordinal);
    }
}
#endif
