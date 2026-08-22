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
    public partial class AndroidDocumentTreeDeviceToCloudLocalTreeReader(
        TimeProvider timeProvider,
        ICottonContentRevisionStore revisionStore) :
        ICottonDeviceToCloudLocalTreeReader
    {
        private const int DocumentIdColumnIndex = 0;
        private const int DisplayNameColumnIndex = 1;
        private const int MimeTypeColumnIndex = 2;
        private const int LastModifiedColumnIndex = 3;
        private const int SizeColumnIndex = 4;

        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        private readonly ICottonContentRevisionStore _revisionStore =
            revisionStore ?? throw new ArgumentNullException(nameof(revisionStore));

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

            CottonContentRevisionIndexSnapshot? storedIndex = await _revisionStore
                .LoadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonContentRevisionIndexSnapshot? previousIndex = string.Equals(
                storedIndex?.SourceVersion,
                RevisionSourceVersion,
                StringComparison.Ordinal)
                    ? storedIndex
                    : null;
            (CottonDeviceToCloudLocalContentSnapshot content, CottonContentRevisionIndexSnapshot revisionIndex) =
                await Task.Run(
                    () => ReadTree(root, previousIndex, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!revisionIndex.HasSameContentAs(storedIndex))
            {
                await _revisionStore
                    .SaveAsync(instanceUri, root, revisionIndex, cancellationToken)
                    .ConfigureAwait(false);
            }

            return content;
        }

        private (CottonDeviceToCloudLocalContentSnapshot Content, CottonContentRevisionIndexSnapshot RevisionIndex)
            ReadTree(
            CottonSyncRootSnapshot root,
            CottonContentRevisionIndexSnapshot? previousIndex,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidUri rootUri = GetRootDocumentUri(treeUri);
            List<CottonDeviceToCloudLocalItemSnapshot> items = [];
            List<CottonDeviceToCloudLocalProblemSnapshot> problems = [];
            List<CottonContentRevisionSnapshot> revisions = [];
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
                previousIndex,
                revisions,
                scanStartedAtUtc,
                traversalGuard,
                depth: 0,
                cancellationToken: cancellationToken);

            CottonDeviceToCloudLocalContentSnapshot content = new(
                root.LocalRoot.DisplayName,
                items,
                problems);
            CottonContentRevisionIndexSnapshot revisionIndex = new(
                RevisionSourceVersion,
                revisions);
            return (content, revisionIndex);
        }

        private static void ReadChildren(
            ContentResolver resolver,
            AndroidUri treeUri,
            AndroidUri parentUri,
            string parentPath,
            List<CottonDeviceToCloudLocalItemSnapshot> items,
            List<CottonDeviceToCloudLocalProblemSnapshot> problems,
            CottonContentRevisionIndexSnapshot? previousIndex,
            List<CottonContentRevisionSnapshot> revisions,
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
                long? lastModifiedMilliseconds = ReadLastModifiedMilliseconds(cursor);
                DateTime updatedAtUtc = lastModifiedMilliseconds.HasValue
                    ? DateTimeOffset.FromUnixTimeMilliseconds(lastModifiedMilliseconds.Value).UtcDateTime
                    : scanStartedAtUtc;
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
                        previousIndex,
                        revisions,
                        scanStartedAtUtc,
                        traversalGuard,
                        depth + 1,
                        cancellationToken);
                    continue;
                }

                long? sizeBytes = ReadSizeBytes(cursor);
                string contentHash = ResolveContentHash(
                    resolver,
                    child,
                    lastModifiedMilliseconds,
                    sizeBytes,
                    previousIndex,
                    revisions,
                    cancellationToken);
                items.Add(CottonDeviceToCloudLocalItemSnapshot.CreateFile(
                    child.DisplayName,
                    relativePath!,
                    updatedAtUtc,
                    sizeBytes,
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
    }
}
#endif
