// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Cotton.Mobile.Services;

namespace Cotton.Mobile.Platforms.Android
{
    public partial class AndroidDocumentTreeDeviceToCloudLocalTreeReader
    {
        private const string RevisionSourceVersion = "android-document-tree-v1";

        private static long? ReadLastModifiedMilliseconds(ICursor cursor)
        {
            if (cursor.IsNull(LastModifiedColumnIndex))
            {
                return null;
            }

            long milliseconds = cursor.GetLong(LastModifiedColumnIndex);
            return milliseconds <= 0 ? null : milliseconds;
        }

        private static long? ReadSizeBytes(ICursor cursor)
        {
            if (cursor.IsNull(SizeColumnIndex))
            {
                return null;
            }

            long sizeBytes = cursor.GetLong(SizeColumnIndex);
            return sizeBytes < 0 ? null : sizeBytes;
        }

        private static string ResolveContentHash(
            ContentResolver resolver,
            AndroidMediaContentAccess contentAccess,
            AndroidDocumentTreeChild child,
            long? lastModifiedMilliseconds,
            long? sizeBytes,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot> revisions,
            CancellationToken cancellationToken)
        {
            contentAccess.EnsureUnchanged();
            if (lastModifiedMilliseconds.HasValue
                && sizeBytes.HasValue
                && previousIndex is not null
                && previousIndex.TryGetContentHash(
                    child.DocumentId,
                    lastModifiedMilliseconds.Value,
                    sizeBytes.Value,
                    out string? cachedHash))
            {
                revisions.Add(new CottonContentRevisionSnapshot(
                    child.DocumentId,
                    lastModifiedMilliseconds.Value,
                    cachedHash,
                    sizeBytes.Value));
                return cachedHash;
            }

            string contentHash = contentAccess.ComputeContentHash(resolver, child.Uri, cancellationToken);
            if (lastModifiedMilliseconds.HasValue && sizeBytes.HasValue)
            {
                revisions.Add(new CottonContentRevisionSnapshot(
                    child.DocumentId,
                    lastModifiedMilliseconds.Value,
                    contentHash,
                    sizeBytes.Value));
            }

            return contentHash;
        }
    }
}
#endif
