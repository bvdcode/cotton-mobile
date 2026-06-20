#if ANDROID
using Android.Content;
using Android.Database;
using Android.Provider;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    public class AndroidDocumentTreeCloudToDeviceSyncFileOperator :
        ICottonUserSelectedDocumentTreeCloudToDeviceSyncFileOperator
    {
        private const int CopyBufferSize = 81920;
        private const string TemporaryFileSuffix = ".cotton-sync-tmp";
        private const string DefaultContentType = "application/octet-stream";

        private static readonly string[] ChildProjection =
        [
            DocumentsContract.Document.ColumnDocumentId,
            DocumentsContract.Document.ColumnDisplayName,
            DocumentsContract.Document.ColumnMimeType,
        ];

        private readonly ICottonFileBrowserService _fileBrowserService;

        public AndroidDocumentTreeCloudToDeviceSyncFileOperator(ICottonFileBrowserService fileBrowserService)
        {
            ArgumentNullException.ThrowIfNull(fileBrowserService);

            _fileBrowserService = fileBrowserService;
        }

        public async Task DownloadOrReplaceAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            CottonFileBrowserEntry file = CreateFileEntry(item);
            CottonFileDownloadResult download = await _fileBrowserService
                .DownloadAsync(instanceUri, file, progress: null, cancellationToken)
                .ConfigureAwait(false);
            await Task.Run(
                    () => WriteFile(root, item, download.FilePath, cancellationToken),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public Task RenameAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            return Task.Run(
                () => RenameFile(root, item, cancellationToken),
                cancellationToken);
        }

        public Task RemoveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            EnsureSupportedRoot(instanceUri, root);
            return Task.Run(
                () => RemoveFile(root, item, cancellationToken),
                cancellationToken);
        }

        private static void WriteFile(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            string sourcePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidUri parentUri = EnsureParentFolder(resolver, treeUri, item.RelativePath, cancellationToken);
            AndroidDocumentTreeChild? existing = FindChild(resolver, treeUri, parentUri, item.DisplayName, cancellationToken);
            if (existing?.IsDirectory == true)
            {
                throw new IOException($"A folder already exists at {item.RelativePath}.");
            }

            if (existing is null)
            {
                AndroidUri documentUri = CreateDocument(resolver, parentUri, item);
                CopyFileToDocument(resolver, sourcePath, documentUri, cancellationToken);
                return;
            }

            string temporaryName = CreateTemporaryName(item.DisplayName);
            AndroidUri temporaryUri = CreateDocument(resolver, parentUri, item.ContentType, temporaryName);
            try
            {
                CopyFileToDocument(resolver, sourcePath, temporaryUri, cancellationToken);
                DocumentsContract.DeleteDocument(resolver, existing.Uri);
                AndroidUri? renamedUri = DocumentsContract.RenameDocument(resolver, temporaryUri, item.DisplayName);
                if (renamedUri is null)
                {
                    throw new IOException($"Could not replace document {item.RelativePath}.");
                }
            }
            catch
            {
                TryDeleteDocument(resolver, temporaryUri);
                throw;
            }
        }

        private static void RenameFile(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(item.PreviousRelativePath))
            {
                throw new InvalidOperationException("Document-tree rename requires the previous relative path.");
            }

            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidDocumentTreeChild source = ResolveExistingFile(
                resolver,
                treeUri,
                item.PreviousRelativePath,
                cancellationToken);

            string previousParentPath = GetParentPath(item.PreviousRelativePath);
            string targetParentPath = GetParentPath(item.RelativePath);
            AndroidUri targetParentUri = EnsureParentFolder(resolver, treeUri, item.RelativePath, cancellationToken);
            AndroidDocumentTreeChild? target = FindChild(resolver, treeUri, targetParentUri, item.DisplayName, cancellationToken);
            if (target?.IsDirectory == true)
            {
                throw new IOException($"A folder already exists at {item.RelativePath}.");
            }

            if (!string.Equals(previousParentPath, targetParentPath, StringComparison.Ordinal))
            {
                CopyDocumentToTarget(resolver, source.Uri, targetParentUri, target, item, cancellationToken);
                DocumentsContract.DeleteDocument(resolver, source.Uri);
                return;
            }

            if (target is not null && !AreSameDocument(source, target))
            {
                DocumentsContract.DeleteDocument(resolver, target.Uri);
            }

            AndroidUri? renamedUri = DocumentsContract.RenameDocument(resolver, source.Uri, item.DisplayName);
            if (renamedUri is null)
            {
                throw new IOException($"Could not rename document {item.PreviousRelativePath}.");
            }
        }

        private static void RemoveFile(
            CottonSyncRootSnapshot root,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ContentResolver resolver = GetContentResolver();
            AndroidUri treeUri = ParseTreeUri(root);
            AndroidDocumentTreeChild? document = ResolveFileOrNull(resolver, treeUri, item.RelativePath, cancellationToken);
            if (document is not null)
            {
                DocumentsContract.DeleteDocument(resolver, document.Uri);
            }
        }

        private static void CopyDocumentToTarget(
            ContentResolver resolver,
            AndroidUri sourceUri,
            AndroidUri targetParentUri,
            AndroidDocumentTreeChild? existingTarget,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            string temporaryName = CreateTemporaryName(item.DisplayName);
            AndroidUri temporaryUri = CreateDocument(resolver, targetParentUri, item.ContentType, temporaryName);
            try
            {
                using Stream source = OpenInputStream(resolver, sourceUri);
                using Stream target = OpenOutputStream(resolver, temporaryUri);
                source.CopyTo(target, CopyBufferSize);
                target.Flush();
                cancellationToken.ThrowIfCancellationRequested();
                if (existingTarget is not null)
                {
                    DocumentsContract.DeleteDocument(resolver, existingTarget.Uri);
                }

                AndroidUri? renamedUri = DocumentsContract.RenameDocument(resolver, temporaryUri, item.DisplayName);
                if (renamedUri is null)
                {
                    throw new IOException($"Could not move document to {item.RelativePath}.");
                }
            }
            catch
            {
                TryDeleteDocument(resolver, temporaryUri);
                throw;
            }
        }

        private static AndroidUri EnsureParentFolder(
            ContentResolver resolver,
            AndroidUri treeUri,
            string fileRelativePath,
            CancellationToken cancellationToken)
        {
            AndroidUri current = GetRootDocumentUri(treeUri);
            foreach (string segment in GetParentSegments(fileRelativePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild? child = FindChild(resolver, treeUri, current, segment, cancellationToken);
                if (child is null)
                {
                    current = CreateDocument(resolver, current, DocumentsContract.Document.MimeTypeDir, segment);
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

        private static AndroidDocumentTreeChild ResolveExistingFile(
            ContentResolver resolver,
            AndroidUri treeUri,
            string relativePath,
            CancellationToken cancellationToken)
        {
            AndroidDocumentTreeChild? file = ResolveFileOrNull(resolver, treeUri, relativePath, cancellationToken);
            return file ?? throw new FileNotFoundException($"Document-tree synced file was not found at {relativePath}.");
        }

        private static AndroidDocumentTreeChild? ResolveFileOrNull(
            ContentResolver resolver,
            AndroidUri treeUri,
            string relativePath,
            CancellationToken cancellationToken)
        {
            AndroidUri? parentUri = ResolveParentFolderOrNull(resolver, treeUri, relativePath, cancellationToken);
            if (parentUri is null)
            {
                return null;
            }
            string fileName = CottonSyncRelativePath.GetFileName(relativePath);
            AndroidDocumentTreeChild? child = FindChild(resolver, treeUri, parentUri, fileName, cancellationToken);
            if (child?.IsDirectory == true)
            {
                throw new IOException($"A folder exists where file {relativePath} is expected.");
            }

            return child;
        }

        private static AndroidUri? ResolveParentFolderOrNull(
            ContentResolver resolver,
            AndroidUri treeUri,
            string fileRelativePath,
            CancellationToken cancellationToken)
        {
            AndroidUri current = GetRootDocumentUri(treeUri);
            foreach (string segment in GetParentSegments(fileRelativePath))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AndroidDocumentTreeChild? child = FindChild(resolver, treeUri, current, segment, cancellationToken);
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

        private static AndroidDocumentTreeChild? FindChild(
            ContentResolver resolver,
            AndroidUri treeUri,
            AndroidUri parentUri,
            string displayName,
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
                string childName = cursor.GetString(1) ?? string.Empty;
                if (!string.Equals(childName, displayName, StringComparison.Ordinal))
                {
                    continue;
                }

                string documentId = cursor.GetString(0)
                    ?? throw new IOException("Document-tree child id is unavailable.");
                string mimeType = cursor.GetString(2) ?? string.Empty;
                AndroidUri childUri = DocumentsContract.BuildDocumentUriUsingTree(treeUri, documentId)
                    ?? throw new IOException("Could not build document-tree child URI.");
                return new AndroidDocumentTreeChild(childUri, documentId, childName, mimeType);
            }

            return null;
        }

        private static void CopyFileToDocument(
            ContentResolver resolver,
            string sourcePath,
            AndroidUri documentUri,
            CancellationToken cancellationToken)
        {
            using var source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize);
            using Stream target = OpenOutputStream(resolver, documentUri);
            source.CopyTo(target, CopyBufferSize);
            target.Flush();
            cancellationToken.ThrowIfCancellationRequested();
        }

        private static Stream OpenInputStream(ContentResolver resolver, AndroidUri documentUri)
        {
            return resolver.OpenInputStream(documentUri)
                ?? throw new IOException("Could not open document for reading.");
        }

        private static Stream OpenOutputStream(ContentResolver resolver, AndroidUri documentUri)
        {
            return resolver.OpenOutputStream(documentUri, "w")
                ?? throw new IOException("Could not open document for writing.");
        }

        private static AndroidUri CreateDocument(
            ContentResolver resolver,
            AndroidUri parentUri,
            CottonCloudToDeviceSyncPlanItem item)
        {
            return CreateDocument(resolver, parentUri, item.ContentType, item.DisplayName);
        }

        private static AndroidUri CreateDocument(
            ContentResolver resolver,
            AndroidUri parentUri,
            string? contentType,
            string displayName)
        {
            AndroidUri? documentUri = DocumentsContract.CreateDocument(
                resolver,
                parentUri,
                string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType,
                displayName);
            return documentUri ?? throw new IOException($"Could not create document {displayName}.");
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

        private static IReadOnlyList<string> GetParentSegments(string fileRelativePath)
        {
            string parentPath = GetParentPath(fileRelativePath);
            return string.IsNullOrWhiteSpace(parentPath)
                ? []
                : parentPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        private static string GetParentPath(string fileRelativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(fileRelativePath, nameof(fileRelativePath));
            int separatorIndex = normalizedPath.LastIndexOf('/');
            return separatorIndex < 0 ? string.Empty : normalizedPath[..separatorIndex];
        }

        private static bool AreSameDocument(AndroidDocumentTreeChild first, AndroidDocumentTreeChild second)
        {
            return string.Equals(first.DocumentId, second.DocumentId, StringComparison.Ordinal);
        }

        private static string CreateTemporaryName(string displayName)
        {
            return $"{displayName}.{Guid.NewGuid():N}{TemporaryFileSuffix}";
        }

        private static void TryDeleteDocument(ContentResolver resolver, AndroidUri documentUri)
        {
            try
            {
                DocumentsContract.DeleteDocument(resolver, documentUri);
            }
            catch (Exception exception)
                when (exception is IOException
                    or UnauthorizedAccessException
                    or Java.IO.FileNotFoundException
                    or Java.Lang.SecurityException)
            {
            }
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
                throw new InvalidOperationException("Cloud-to-device sync instance does not match the sync root.");
            }

            if (!root.CanRunSync)
            {
                throw new InvalidOperationException("Cloud-to-device sync root is not ready.");
            }

            if (!root.LocalRoot.RequiresPersistedUserGrant)
            {
                throw new InvalidOperationException("This sync file operator only supports user-selected folders.");
            }

            if (root.Direction == CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("This sync file operator requires cloud-to-device sync direction.");
            }
        }

        private static CottonFileBrowserEntry CreateFileEntry(CottonCloudToDeviceSyncPlanItem item)
        {
            ArgumentNullException.ThrowIfNull(item);
            if (item.TargetType != CottonFileBrowserEntryType.File)
            {
                throw new InvalidOperationException("Only files can be written by cloud-to-device sync.");
            }

            if (string.IsNullOrWhiteSpace(item.RemoteETag) || !item.RemoteUpdatedAtUtc.HasValue)
            {
                throw new InvalidOperationException("Cloud-to-device file writes require a remote ETag and update time.");
            }

            return CottonFileBrowserEntry.CreateFile(
                item.TargetId,
                item.DisplayName,
                item.RemoteUpdatedAtUtc.Value,
                item.SizeBytes,
                item.ContentType,
                previewHashEncryptedHex: null,
                item.RemoteETag);
        }

    }
}
#endif
