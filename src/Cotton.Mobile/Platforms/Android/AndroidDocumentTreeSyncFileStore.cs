// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content;
using System.Runtime.ExceptionServices;
using AndroidUri = Android.Net.Uri;

namespace Cotton.Mobile.Services
{
    internal class AndroidDocumentTreeSyncFileStore
    {
        private const int CopyBufferSize = 81920;
        private const string DefaultContentType = "application/octet-stream";

        private readonly ContentResolver _resolver;
        private readonly AndroidDocumentTreeNavigator _navigator;
        private readonly AndroidDocumentMutationStore _mutationStore;
        private readonly CottonRecoverableDocumentReplacement<AndroidUri> _replacement;

        public AndroidDocumentTreeSyncFileStore(ContentResolver resolver, AndroidUri treeUri)
        {
            ArgumentNullException.ThrowIfNull(resolver);
            ArgumentNullException.ThrowIfNull(treeUri);

            _resolver = resolver;
            _navigator = new AndroidDocumentTreeNavigator(resolver, treeUri);
            _mutationStore = new AndroidDocumentMutationStore(resolver);
            _replacement = new CottonRecoverableDocumentReplacement<AndroidUri>(_mutationStore);
        }

        public void Write(
            CottonCloudToDeviceSyncPlanItem item,
            string sourcePath,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AndroidUri parentUri = _navigator.EnsureParentFolder(item.RelativePath, cancellationToken);
            AndroidDocumentTreeChild? current = _navigator.FindChild(
                parentUri,
                item.DisplayName,
                cancellationToken);
            if (current?.IsDirectory == true)
            {
                throw new IOException($"A folder already exists at {item.RelativePath}.");
            }

            AndroidUri temporaryUri = CreateVerifiedTemporaryDocument(
                parentUri,
                item,
                target => CopyFileToStream(sourcePath, target, cancellationToken),
                cancellationToken);
            Promote(temporaryUri, current, item.DisplayName);
        }

        public void Rename(CottonCloudToDeviceSyncPlanItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string previousRelativePath = item.PreviousRelativePath
                ?? throw new InvalidOperationException("Document-tree rename requires the previous relative path.");
            AndroidDocumentTreeChild source = _navigator.ResolveExistingFile(
                previousRelativePath,
                cancellationToken);
            VerifyDocumentContent(source.Uri, item, cancellationToken);
            string previousParentPath = AndroidDocumentTreeNavigator.GetParentPath(previousRelativePath);
            string targetParentPath = AndroidDocumentTreeNavigator.GetParentPath(item.RelativePath);
            AndroidUri targetParentUri = _navigator.EnsureParentFolder(item.RelativePath, cancellationToken);
            AndroidDocumentTreeChild? target = _navigator.FindChild(
                targetParentUri,
                item.DisplayName,
                cancellationToken);
            if (target?.IsDirectory == true)
            {
                throw new IOException($"A folder already exists at {item.RelativePath}.");
            }

            if (!string.Equals(previousParentPath, targetParentPath, StringComparison.Ordinal))
            {
                CopyToTarget(source.Uri, targetParentUri, target, item, cancellationToken);
                return;
            }

            if (target is null)
            {
                _mutationStore.Rename(source.Uri, item.DisplayName);
                return;
            }

            if (string.Equals(source.DocumentId, target.DocumentId, StringComparison.Ordinal))
            {
                return;
            }

            Replace(source.Uri, target.Uri, item.DisplayName);
        }

        public void Remove(CottonCloudToDeviceSyncPlanItem item, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AndroidDocumentTreeChild? document = _navigator.ResolveFileOrNull(
                item.RelativePath,
                cancellationToken);
            if (document is not null)
            {
                VerifyDocumentContent(document.Uri, item, cancellationToken);
                _mutationStore.Delete(document.Uri);
            }
        }

        private void CopyToTarget(
            AndroidUri sourceUri,
            AndroidUri targetParentUri,
            AndroidDocumentTreeChild? currentTarget,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            AndroidUri temporaryUri = CreateVerifiedTemporaryDocument(
                targetParentUri,
                item,
                target =>
                {
                    using Stream source = OpenInputStream(sourceUri);
                    CopyStream(source, target, cancellationToken);
                },
                cancellationToken);
            Promote(temporaryUri, currentTarget, item.DisplayName);
            _mutationStore.Delete(sourceUri);
        }

        private void Promote(
            AndroidUri temporaryUri,
            AndroidDocumentTreeChild? current,
            string displayName)
        {
            if (current is null)
            {
                PromoteNewDocument(temporaryUri, displayName);
                return;
            }

            Replace(temporaryUri, current.Uri, displayName);
        }

        private void Replace(AndroidUri replacementUri, AndroidUri currentUri, string displayName)
        {
            _replacement.Replace(
                replacementUri,
                currentUri,
                displayName,
                CottonSyncWorkingFileName.CreateBackup(displayName));
        }

        private AndroidUri CreateVerifiedTemporaryDocument(
            AndroidUri parentUri,
            CottonCloudToDeviceSyncPlanItem item,
            Action<Stream> writeContent,
            CancellationToken cancellationToken)
        {
            string temporaryName = CottonSyncWorkingFileName.CreateTemporary(item.DisplayName);
            string contentType = string.IsNullOrWhiteSpace(item.ContentType)
                ? DefaultContentType
                : item.ContentType;
            AndroidUri temporaryUri = _navigator.CreateDocument(parentUri, contentType, temporaryName);
            try
            {
                using (Stream target = OpenOutputStream(temporaryUri))
                {
                    writeContent(target);
                    target.Flush();
                }

                cancellationToken.ThrowIfCancellationRequested();
                VerifyDocumentContent(temporaryUri, item, cancellationToken);
                return temporaryUri;
            }
            catch (Exception exception)
            {
                DeleteFailedTemporaryDocument(temporaryUri, exception);
                throw;
            }
        }

        private void VerifyDocumentContent(
            AndroidUri documentUri,
            CottonCloudToDeviceSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            string expectedContentHash = item.ContentHash
                ?? throw new InvalidOperationException("Document-tree sync requires a remote content hash.");
            using Stream content = OpenInputStream(documentUri);
            string contentHash = CottonContentHash.ComputeSha256(content, cancellationToken);
            if (!string.Equals(contentHash, expectedContentHash, StringComparison.Ordinal))
            {
                throw new IOException($"Copied document content hash mismatch for {item.RelativePath}.");
            }
        }

        private void PromoteNewDocument(AndroidUri temporaryUri, string displayName)
        {
            try
            {
                _mutationStore.Rename(temporaryUri, displayName);
            }
            catch (Exception exception)
            {
                DeleteFailedTemporaryDocument(temporaryUri, exception);
                throw;
            }
        }

        private void DeleteFailedTemporaryDocument(AndroidUri temporaryUri, Exception originalException)
        {
            try
            {
                _mutationStore.Delete(temporaryUri);
            }
            catch (Exception cleanupException)
            {
                throw new System.AggregateException(
                    "Document write failed and temporary-document cleanup also failed.",
                    originalException,
                    cleanupException);
            }

            ExceptionDispatchInfo.Capture(originalException).Throw();
        }

        private static void CopyFileToStream(
            string sourcePath,
            Stream target,
            CancellationToken cancellationToken)
        {
            using FileStream source = new FileStream(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                CopyBufferSize);
            CopyStream(source, target, cancellationToken);
        }

        private static void CopyStream(Stream source, Stream target, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[CopyBufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead = source.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    return;
                }

                target.Write(buffer, 0, bytesRead);
            }
        }

        private Stream OpenInputStream(AndroidUri documentUri)
        {
            return _resolver.OpenInputStream(documentUri)
                ?? throw new IOException("Could not open document for reading.");
        }

        private Stream OpenOutputStream(AndroidUri documentUri)
        {
            return _resolver.OpenOutputStream(documentUri, "w")
                ?? throw new IOException("Could not open document for writing.");
        }
    }
}
#endif
