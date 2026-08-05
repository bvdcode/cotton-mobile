// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    internal class AndroidDocumentTreeNavigator
    {
        private static readonly string[] ChildProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType,
        ];

        private readonly ContentResolver _resolver;
        private readonly AndroidUri _treeUri;
        private readonly AndroidUri _rootDocumentUri;

        public AndroidDocumentTreeNavigator(ContentResolver resolver, AndroidUri treeUri)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(treeUri);

            _resolver = resolver;
            _treeUri = treeUri;
            string rootDocumentId = DocumentsContract.GetTreeDocumentId(treeUri)
                ?? throw new InvalidOperationException("Document-tree root id is unavailable.");
            _rootDocumentUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, rootDocumentId)
                ?? throw new InvalidOperationException("Could not build document-tree root URI.");
        }

        public AndroidUri EnsureParentFolder(string fileRelativePath, CancellationToken cancellationToken)
        {
            AndroidUri current = _rootDocumentUri;
            foreach (string segment in GetParentSegments(fileRelativePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild? child = FindChild(current, segment, cancellationToken);
                if (child is null)
                {
                    current = CreateDocument(current, DocumentsContract.Document.MimeTypeDir, segment);
                    continue;
                }

                if (!child.IsDirectory)
                {
                    throw new IOException($"A file already exists where folder {segment} is required.");
                }

                current = child.Uri;
            }

            return current;
        }

        public AndroidDocumentTreeChild ResolveExistingFile(
            string relativePath,
            CancellationToken cancellationToken)
        {
            AndroidDocumentTreeChild? file = ResolveFileOrNull(relativePath, cancellationToken);
            return file ?? throw new FileNotFoundException(
                $"Document-tree synced file was not found at {relativePath}.");
        }

        public AndroidDocumentTreeChild? ResolveFileOrNull(
            string relativePath,
            CancellationToken cancellationToken)
        {
            AndroidUri? parentUri = ResolveParentFolderOrNull(relativePath, cancellationToken);
            if (parentUri is null)
            {
                return null;
            }

            string fileName = CottonSyncRelativePath.GetFileName(relativePath);
            AndroidDocumentTreeChild? child = FindChild(parentUri, fileName, cancellationToken);
            if (child?.IsDirectory == true)
            {
                throw new IOException($"A folder exists where file {relativePath} is expected.");
            }

            return child;
        }

        public AndroidDocumentTreeChild? FindChild(
            AndroidUri parentUri,
            string displayName,
            CancellationToken cancellationToken)
        {
            string parentDocumentId = DocumentsContract.GetDocumentId(parentUri)
                ?? throw new IOException("Document-tree parent id is unavailable.");
            AndroidUri childrenUri = DocumentsContract.BuildChildDocumentsUriUsingTree(_treeUri, parentDocumentId)
                ?? throw new IOException("Could not build document-tree children URI.");
            using ICursor? cursor = _resolver.Query(childrenUri, ChildProjection, null, null, null);
            if (cursor is null)
            {
                throw new IOException("Could not read document-tree children.");
            }

            while (cursor.MoveToNext())
            {
                cancellationToken.ThrowIfCancellationRequested();
                string childName = cursor.GetString(1) ?? string.Empty;
                if (!string.Equals(childName, displayName, StringComparison.Ordinal))
                {
                    continue;
                }

                string documentId = cursor.GetString(0)
                    ?? throw new IOException("Document-tree child id is unavailable.");
                string mimeType = cursor.GetString(2) ?? string.Empty;
                AndroidUri childUri = DocumentsContract.BuildDocumentUriUsingTree(_treeUri, documentId)
                    ?? throw new IOException("Could not build document-tree child URI.");
                return new AndroidDocumentTreeChild(childUri, documentId, childName, mimeType);
            }

            return null;
        }

        public AndroidUri CreateDocument(AndroidUri parentUri, string contentType, string displayName)
        {
            AndroidUri? documentUri = DocumentsContract.CreateDocument(
                _resolver,
                parentUri,
                contentType,
                displayName);
            return documentUri ?? throw new IOException($"Could not create document {displayName}.");
        }

        public static string GetParentPath(string fileRelativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(
                fileRelativePath,
                nameof(fileRelativePath));
            int separatorIndex = normalizedPath.LastIndexOf('/');
            return separatorIndex < 0 ? string.Empty : normalizedPath[..separatorIndex];
        }

        private AndroidUri? ResolveParentFolderOrNull(
            string fileRelativePath,
            CancellationToken cancellationToken)
        {
            AndroidUri current = _rootDocumentUri;
            foreach (string segment in GetParentSegments(fileRelativePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild? child = FindChild(current, segment, cancellationToken);
                if (child is null)
                {
                    return null;
                }

                if (!child.IsDirectory)
                {
                    throw new IOException($"A file exists where folder {segment} is expected.");
                }

                current = child.Uri;
            }

            return current;
        }

        private static IReadOnlyList<string> GetParentSegments(string fileRelativePath)
        {
            string parentPath = GetParentPath(fileRelativePath);
            return string.IsNullOrWhiteSpace(parentPath)
                ? []
                : parentPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
#endif
