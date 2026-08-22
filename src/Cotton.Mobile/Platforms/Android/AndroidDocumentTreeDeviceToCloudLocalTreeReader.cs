// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidDocumentTreeDeviceToCloudLocalTreeReader(TimeProvider timeProvider) :
        ICottonDeviceToCloudLocalTreeReader
    {
        private const int DocumentIdColumnIndex = 0;
        private const int DisplayNameColumnIndex = 1;
        private const int MimeTypeColumnIndex = 2;
        private const int LastModifiedColumnIndex = 3;
        private const int SizeColumnIndex = 4;

        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

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

        private CottonDeviceToCloudLocalContentSnapshot ReadTree(
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidUri rootUri = GetRootDocumentUri(treeUri);
            List<CottonDeviceToCloudLocalItemSnapshot> items = [];
            List<CottonDeviceToCloudLocalProblemSnapshot> problems = [];
            CottonSyncTraversalGuard<string> traversalGuard = new();
            DateTime scanStartedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            string rootDocumentId = DocumentsContract.GetDocumentId(rootUri)
                ?? throw new IOException("Document-tree root id is unavailable.");
            traversalGuard.TryEnterContainer(rootDocumentId, 0);

            ReadChildren(
                resolver,
                treeUri,
                rootUri,
                parentPath: string.Empty,
                items,
                problems,
                scanStartedAtUtc,
                traversalGuard,
                depth: 0,
                cancellationToken: cancellationToken);

            return new CottonDeviceToCloudLocalContentSnapshot(root.LocalRoot.DisplayName, items, problems);
        }

        private static void ReadChildren(
            ContentResolver resolver,
            AndroidUri treeUri,
            AndroidUri parentUri,
            string parentPath,
            List<CottonDeviceToCloudLocalItemSnapshot> items,
            List<CottonDeviceToCloudLocalProblemSnapshot> problems,
            DateTime scanStartedAtUtc,
            CottonSyncTraversalGuard<string> traversalGuard,
            int depth,
            CancellationToken cancellationToken)
        {
            string parentDocumentId = DocumentsContract.GetDocumentId(parentUri)
                ?? throw new IOException("Document-tree parent id is unavailable.");
            AndroidUri childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(treeUri, parentDocumentId)
                ?? throw new IOException("Could not build document-tree children URI.");
            using ICursor? cursor = resolver.Query(childrenUri, ChildProjection, null, null, null) ?? throw new IOException("Could not read document-tree children.");
            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild child = ReadChild(treeUri, cursor);
                if (!child.IsDirectory && CottonSyncIgnoredFileName.IsIgnored(child.DisplayName))
                {
                    continue;
                }

                traversalGuard.RecordItem();
                DateTime updatedAtUtc = ReadLastModifiedUtc(cursor, scanStartedAtUtc);
                string rawRelativePath = CreateRawRelativePath(parentPath, child.DisplayName);
                if (!TryCreateRelativePath(parentPath, child.DisplayName, out string? relativePath))
                {
                    problems.Add(CreateInvalidNameProblem(child, rawRelativePath));
                    continue;
                }

                if (child.IsDirectory)
                {
                    if (!traversalGuard.TryEnterContainer(child.DocumentId, depth + 1))
                    {
                        continue;
                    }

                    items.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFolder(
                        child.DisplayName,
                        relativePath!,
                        updatedAtUtc,
                        child.DocumentId));
                    ReadChildren(
                        resolver,
                        treeUri,
                        child.Uri,
                        relativePath!,
                        items,
                        problems,
                        scanStartedAtUtc,
                        traversalGuard,
                        depth + 1,
                        cancellationToken);
                    continue;
                }

                string contentHash = ComputeContentHash(resolver, child.Uri, cancellationToken);
                items.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                    child.DisplayName,
                    relativePath!,
                    updatedAtUtc,
                    ReadSizeBytes(cursor),
                    child.MimeType,
                    child.DocumentId,
                    contentHash));
            }
        }

        private static bool TryCreateRelativePath(string parentPath, string displayName, out string? relativePath)
        {
            try
            {
                relativePath = CottonSyncRelativePath.CreateFilePath(parentPath, displayName);
                return true;
            }
            catch (ArgumentException)
            {
                relativePath = null;
                return false;
            }
        }

        private static CottonDeviceToCloudLocalProblemSnapshot CreateInvalidNameProblem(
            AndroidDocumentTreeChild child,
            string rawRelativePath)
        {
            return new CottonDeviceToCloudLocalProblemSnapshot(
                CottonDeviceToCloudLocalProblemKind.InvalidCloudName,
                child.IsDirectory ? CottonFileBrowserEntryType.Folder : CottonFileBrowserEntryType.File,
                CreateProblemDisplayName(child.DisplayName),
                rawRelativePath,
                CoreResources.UnsyncableName);
        }

        private static string CreateProblemDisplayName(string displayName)
        {
            return string.IsNullOrWhiteSpace(displayName) ? CoreResources.UnnamedName : displayName.Trim();
        }

        private static string CreateRawRelativePath(string parentPath, string displayName)
        {
            string trimmedName = CreateProblemDisplayName(displayName);
            return string.IsNullOrWhiteSpace(parentPath)
                ? trimmedName
                : $"{parentPath}/{trimmedName}";
        }

        private static AndroidDocumentTreeChild ReadChild(AndroidUri treeUri, ICursor cursor)
        {
            string documentId = cursor.GetString(DocumentIdColumnIndex)
                ?? throw new IOException("Document-tree child id is unavailable.");
            string displayName = cursor.GetString(DisplayNameColumnIndex)
                ?? throw new IOException("Document-tree child name is unavailable.");
            string mimeType = cursor.GetString(MimeTypeColumnIndex) ?? string.Empty;
            AndroidUri childUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                ?? throw new IOException("Could not build document-tree child URI.");
            return new AndroidDocumentTreeChild(childUri, documentId, displayName, mimeType);
        }

        private static DateTime ReadLastModifiedUtc(ICursor cursor, DateTime scanStartedAtUtc)
        {
            if (cursor.IsNull(LastModifiedColumnIndex))
            {
                return scanStartedAtUtc;
            }

            long milliseconds = cursor.GetLong(LastModifiedColumnIndex);
            return milliseconds <= 0
                ? scanStartedAtUtc
                : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
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

        private static string ComputeContentHash(
            ContentResolver resolver,
            AndroidUri documentUri,
            CancellationToken cancellationToken)
        {
            using Stream content = resolver.OpenInputStream(documentUri)
                ?? throw new IOException("Could not open document-tree file content.");
            return CottonContentHash.ComputeSha256(content, cancellationToken);
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
            return global::Android.App.Application.Context.ContentResolver
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

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("Device-to-cloud local tree reading requires device-to-cloud sync direction.");
            }
        }
    }
}
#endif
