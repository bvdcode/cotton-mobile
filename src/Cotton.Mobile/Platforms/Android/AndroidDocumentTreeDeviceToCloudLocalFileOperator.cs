// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeDeviceToCloudLocalFileOperator :
        ICottonDeviceToCloudLocalFileOperator
    {
        private const int DocumentIdColumnIndex = 0;
        private const int DisplayNameColumnIndex = 1;
        private const int MimeTypeColumnIndex = 2;
        private const int LastModifiedColumnIndex = 3;
        private const int SizeColumnIndex = 4;
        private const int FlagsColumnIndex = 5;

        private static readonly string[] DocumentProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType,
            DocumentsContract.Document.ColumnLastModified,
            DocumentsContract.Document.ColumnSize,
            DocumentsContract.Document.ColumnFlags,
        ];

        public async Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedInput(instanceUri, root, item);

            return await Task.Run(
                    () => DeleteIfUnchanged(root, item, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static CottonDeviceToCloudLocalFileDeleteStatus DeleteIfUnchanged(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ContentResolver resolver = GetContentResolver();
                AndroidUri treeUri = ParseTreeUri(root);
                string documentId = item.LocalSourceId
                    ?? throw new InvalidOperationException("Local file deletion requires a document id.");
                AndroidUri documentUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                    ?? throw new IOException("Could not build the local document URI.");
                using ICursor? cursor = resolver.Query(documentUri, DocumentProjection, null, null, null);
                if (cursor is null)
                {
                    return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
                }

                if (!cursor.MoveToFirst())
                {
                    return CottonDeviceToCloudLocalFileDeleteStatus.AlreadyMissing;
                }

                CottonDeviceToCloudLocalFileDeleteStatus? blocker = FindDeleteBlocker(
                    resolver,
                    documentUri,
                    cursor,
                    item,
                    cancellationToken);
                if (blocker.HasValue)
                {
                    return blocker.Value;
                }

                cancellationToken.ThrowIfCancellationRequested();
                if (!DocumentsContract.DeleteDocument(resolver, documentUri))
                {
                    return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
                }

                return CottonDeviceToCloudLocalFileDeleteStatus.Deleted;
            }
            catch (Java.IO.FileNotFoundException)
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.AlreadyMissing;
            }
            catch (Java.Lang.SecurityException)
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
            }
        }

        private static CottonDeviceToCloudLocalFileDeleteStatus? FindDeleteBlocker(
            ContentResolver resolver,
            AndroidUri documentUri,
            ICursor cursor,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            string? documentId = cursor.GetString(DocumentIdColumnIndex);
            string? displayName = cursor.GetString(DisplayNameColumnIndex);
            string? mimeType = cursor.GetString(MimeTypeColumnIndex);
            if (!string.Equals(documentId, item.LocalSourceId, StringComparison.Ordinal)
                || !string.Equals(displayName, item.DisplayName, StringComparison.Ordinal)
                || string.Equals(mimeType, DocumentsContract.Document.MimeTypeDir, StringComparison.Ordinal))
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Changed;
            }

            if (!item.LocalUpdatedAtUtc.HasValue
                || !item.SizeBytes.HasValue
                || cursor.IsNull(LastModifiedColumnIndex)
                || cursor.IsNull(SizeColumnIndex)
                || cursor.IsNull(FlagsColumnIndex))
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
            }

            long lastModifiedMilliseconds = cursor.GetLong(LastModifiedColumnIndex);
            long sizeBytes = cursor.GetLong(SizeColumnIndex);
            if (lastModifiedMilliseconds <= 0 || sizeBytes < 0)
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
            }

            DateTime lastModifiedUtc = DateTimeOffset
                .FromUnixTimeMilliseconds(lastModifiedMilliseconds)
                .UtcDateTime;
            if (CottonLocalFileFreshness.NormalizeUtc(lastModifiedUtc) != item.LocalUpdatedAtUtc.Value
                || sizeBytes != item.SizeBytes.Value)
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Changed;
            }

            DocumentContractFlags flags = (DocumentContractFlags)cursor.GetLong(FlagsColumnIndex);
            if ((flags & DocumentContractFlags.SupportsDelete) == 0)
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Unsupported;
            }

            using Stream content = resolver.OpenInputStream(documentUri)
                ?? throw new IOException("Could not open local document content before deletion.");
            string contentHash = CottonContentHash.ComputeSha256(content, cancellationToken);
            if (!string.Equals(contentHash, item.ContentHash, StringComparison.Ordinal))
            {
                return CottonDeviceToCloudLocalFileDeleteStatus.Changed;
            }

            return null;
        }

        private static AndroidUri ParseTreeUri(CottonSyncRootSnapshot root)
        {
            AndroidUri? uri = AndroidUri.Parse(root.LocalRoot.RootKey);
            return uri ?? throw new InvalidOperationException("Document-tree sync root URI is invalid.");
        }

        private static ContentResolver GetContentResolver()
        {
            return Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
        }

        private static void EnsureSupportedInput(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(item);
            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new InvalidOperationException("Local file deletion instance does not match the sync root.");
            }

            if (!root.CanRunSync
                || root.Direction != CottonSyncDirection.DeviceToCloud
                || !root.DeletesOriginalsAfterUpload
                || !root.LocalRoot.RequiresPersistedUserGrant)
            {
                throw new InvalidOperationException("Local file deletion requires a ready upload-only document-tree root.");
            }

            if (item.TargetType != CottonFileBrowserEntryType.File
                || string.IsNullOrWhiteSpace(item.LocalSourceId)
                || !item.LocalUpdatedAtUtc.HasValue
                || item.ContentHash is null)
            {
                throw new InvalidOperationException("Local file deletion requires an exact local file revision.");
            }

            if (item.Action is not CottonDeviceToCloudSyncActionKind.UploadNewFile
                and not CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload
                and not CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile)
            {
                throw new InvalidOperationException("Local file deletion requires an uploaded-file action.");
            }
        }
    }
}
#endif
