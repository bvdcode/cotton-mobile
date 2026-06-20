#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeDeviceToCloudLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
    {
        private static readonly string[] ChildProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType,
            DocumentsContract.Document.ColumnLastModified,
            DocumentsContract.Document.ColumnSize,
        ];

        public async Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);

            return await Task.Run(
                    () => ReadTree(root, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static CottonDeviceToCloudLocalContentSnapshot ReadTree(
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidUri rootUri = GetRootDocumentUri(treeUri);
            var items = new List<CottonDeviceToCloudLocalItemSnapshot>();
            DateTime scanStartedAtUtc = DateTime.UtcNow;

            ReadChildren(
                resolver,
                treeUri,
                rootUri,
                parentPath: string.Empty,
                items,
                scanStartedAtUtc,
                cancellationToken);

            return new CottonDeviceToCloudLocalContentSnapshot(root.LocalRoot.DisplayName, items);
        }

        private static void ReadChildren(
            ContentResolver resolver,
            AndroidUri treeUri,
            AndroidUri parentUri,
            string parentPath,
            List<CottonDeviceToCloudLocalItemSnapshot> items,
            DateTime scanStartedAtUtc,
            CancellationToken cancellationToken)
        {
            string parentDocumentId = DocumentsContract.GetDocumentId(parentUri)
                ?? throw new IOException("Document-tree parent id is unavailable.");
            AndroidUri childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, parentDocumentId)
                ?? throw new IOException("Could not build document-tree children URI.");
            using ICursor? cursor = resolver.Query(childrenUri, ChildProjection, null, null, null);
            if (cursor is null)
            {
                throw new IOException("Could not read document-tree children.");
            }

            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild child = ReadChild(treeUri, cursor);
                string relativePath = CottonSyncRelativePath.CreateFilePath(parentPath, child.DisplayName);
                DateTime updatedAtUtc = ReadLastModifiedUtc(cursor, scanStartedAtUtc);

                if (child.IsDirectory)
                {
                    items.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFolder(
                        child.DisplayName,
                        relativePath,
                        updatedAtUtc));
                    ReadChildren(
                        resolver,
                        treeUri,
                        child.Uri,
                        relativePath,
                        items,
                        scanStartedAtUtc,
                        cancellationToken);
                    continue;
                }

                items.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                    child.DisplayName,
                    relativePath,
                    updatedAtUtc,
                    ReadSizeBytes(cursor),
                    child.MimeType));
            }
        }

        private static AndroidDocumentTreeChild ReadChild(AndroidUri treeUri, ICursor cursor)
        {
            string documentId = cursor.GetString(0)
                ?? throw new IOException("Document-tree child id is unavailable.");
            string displayName = cursor.GetString(1)
                ?? throw new IOException("Document-tree child name is unavailable.");
            string mimeType = cursor.GetString(2) ?? string.Empty;
            AndroidUri childUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                ?? throw new IOException("Could not build document-tree child URI.");
            return new AndroidDocumentTreeChild(childUri, documentId, displayName, mimeType);
        }

        private static DateTime ReadLastModifiedUtc(ICursor cursor, DateTime scanStartedAtUtc)
        {
            if (cursor.IsNull(3))
            {
                return scanStartedAtUtc;
            }

            long milliseconds = cursor.GetLong(3);
            return milliseconds <= 0
                ? scanStartedAtUtc
                : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
        }

        private static long? ReadSizeBytes(ICursor cursor)
        {
            if (cursor.IsNull(4))
            {
                return null;
            }

            long sizeBytes = cursor.GetLong(4);
            return sizeBytes < 0 ? null : sizeBytes;
        }

        private static AndroidUri ParseTreeUri(CottonSyncRootSnapshot root)
        {
            AndroidUri? uri = AndroidUri.Parse(root.LocalRoot.RootKey);
            return uri ?? throw new InvalidOperationException("Document-tree sync root URI is invalid.");
        }

        private static AndroidUri GetRootDocumentUri(AndroidUri treeUri)
        {
            string rootDocumentId = DocumentsContract.GetTreeDocumentId(treeUri)
                ?? throw new InvalidOperationException("Document-tree root id is unavailable.");
            return DocumentsContract.BuildDocumentUriUsingTree(treeUri, rootDocumentId)
                ?? throw new InvalidOperationException("Could not build document-tree root URI.");
        }

        private static ContentResolver GetContentResolver()
        {
            return Android.App.Application.Context.ContentResolver
                ?? throw new InvalidOperationException("Android content resolver is unavailable.");
        }

        private static void EnsureSupportedRoot(Uri instanceUri, CottonSyncRootSnapshot root)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));
            ArgumentNullException.ThrowIfNull(root);

            if (!string.Equals(
                CottonMobileStoragePaths.CreateInstanceStorageKey(instanceUri),
                CottonMobileStoragePaths.CreateInstanceStorageKey(root.InstanceUri),
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Device-to-cloud sync root is not ready.");
            }

            if (!root.LocalRoot.RequiresPersistedUserGrant)
            {
                throw new InvalidOperationException("Device-to-cloud local tree reading only supports user-selected folders.");
            }

            if (root.Direction == CottonSyncDirection.CloudToDevice)
            {
                throw new InvalidOperationException("Device-to-cloud local tree reading requires device-to-cloud sync direction.");
            }
        }
    }
}
#endif
