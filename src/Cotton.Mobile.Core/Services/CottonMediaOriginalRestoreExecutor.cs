// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreExecutor(
        ICottonMediaOriginalRestoreStore restoreStore,
        ICottonRedactedMediaHashSource redactedHashSource,
        ICottonFileUploadService uploadService,
        CottonSyncFileUploadSourceFactory sourceFactory,
        ICottonRestoredUploadReceiptStore receiptStore,
        CottonSyncProgressHub progressHub,
        TimeProvider timeProvider,
        ILogger<CottonMediaOriginalRestoreExecutor> logger)
    {
        internal async Task<int> ApplyAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent,
            CancellationToken cancellationToken)
        {
            CottonMediaOriginalRestoreState state = await restoreStore.LoadAsync(root, cancellationToken).ConfigureAwait(false);
            if (state.Approvals.Count == 0)
            {
                return 0;
            }

            if (!redactedHashSource.CanReadOriginals)
            {
                throw new UnauthorizedAccessException("Media location permission is required to restore originals.");
            }

            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localBySource = localContent.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File && item.LocalSourceId is not null)
                .ToDictionary(item => item.LocalSourceId!, StringComparer.Ordinal);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById = remoteContent.Items
                .ToDictionary(item => item.Entry.Id);
            CottonDeviceToCloudRemoteFolderIndex folders = new(root, remoteContent);
            int restored = 0;
            foreach (CottonMediaOriginalRestoreApproval approval in state.Approvals)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool matchesLocal = localBySource.TryGetValue(approval.LocalFile.LocalSourceId!, out CottonDeviceToCloudLocalItemSnapshot? local)
                    && local.RelativePath == approval.LocalFile.RelativePath
                    && local.ContentHash == approval.LocalFile.ContentHash && local.SizeBytes == approval.LocalFile.SizeBytes;
                bool matchesPath = remoteById.TryGetValue(approval.FileId, out CottonDeviceToCloudRemoteItemSnapshot? remote)
                    && remote.RelativePath == approval.LocalFile.RelativePath && !remote.Entry.IsFolder;
                if (matchesLocal && matchesPath && local is not null && remote is not null
                    && remote.Entry.SizeBytes == local.SizeBytes
                    && !string.IsNullOrWhiteSpace(remote.Entry.ETag)
                    && (remote.Entry.ContentHash == local.ContentHash
                        || (remote.Entry.ContentHash == approval.RedactedHash && remote.Entry.ETag == approval.ExpectedETag)))
                {
                    CottonFileBrowserEntry result = await RestoreFileAsync(root, approval, local, remote.Entry,
                        folders, restored, state.Approvals.Count, cancellationToken).ConfigureAwait(false);
                    CottonUploadReceiptSnapshot receipt = new(local.LocalSourceId!, local.RelativePath,
                        local.LocalUpdatedAtUtc, local.SizeBytes, local.ContentType, approval.OperationId,
                        CottonUploadReceiptStatus.Uploaded, timeProvider.GetUtcNow().UtcDateTime,
                        result.Id, result.ETag, local.ContentHash);
                    await receiptStore.SaveRestoredAsync(root, receipt, cancellationToken).ConfigureAwait(false);
                    restored++;
                    CottonMediaRestoreLog.FileCompleted(logger, root.Id, approval.FileId, approval.OperationId);
                }
                else
                {
                    CottonMediaRestoreLog.FileChanged(logger, root.Id, approval.FileId);
                }

                await restoreStore.UpdateAsync(root, current => new CottonMediaOriginalRestoreState(
                    root.StableKey, current.ReviewCompleted,
                    [.. current.Approvals.Where(item => item.OperationId != approval.OperationId)]), cancellationToken)
                    .ConfigureAwait(false);
            }

            return restored;
        }

        private async Task<CottonFileBrowserEntry> RestoreFileAsync(
            CottonSyncRootSnapshot root,
            CottonMediaOriginalRestoreApproval approval,
            CottonDeviceToCloudLocalItemSnapshot local,
            CottonFileBrowserEntry remote,
            CottonDeviceToCloudRemoteFolderIndex folders,
            int completed,
            int total,
            CancellationToken cancellationToken)
        {
            if (remote.ContentHash == local.ContentHash)
            {
                return remote;
            }

            CottonMediaRestoreLog.FileStarted(logger, root.Id, approval.FileId, approval.OperationId);
            CottonDeviceToCloudSyncPlanItem upload = CottonDeviceToCloudSyncPlanItemFactory.CreateLocal(
                CottonDeviceToCloudSyncActionKind.UploadNewFile, local, approval.FileId, approval.ExpectedETag)
                .WithUploadOperationId(approval.OperationId);
            CottonFileUploadSource source = sourceFactory.Create(root.InstanceUri, root, upload);
            CottonSyncUploadProgressReporter progress = new(root.Id, upload.DisplayName,
                completed + 1, total, completed, total, upload.SizeBytes, progressHub, timeProvider);
            progress.Report(0);
            CottonFileBrowserEntry updated = await uploadService.UpdateContentAsync(
                root.InstanceUri, approval.FileId, folders.ResolveParent(upload), approval.ExpectedETag,
                source, progress, cancellationToken).ConfigureAwait(false);
            if (updated.Id != approval.FileId || updated.Type != CottonFileBrowserEntryType.File
                || updated.Name != local.DisplayName || updated.SizeBytes != local.SizeBytes
                || updated.ContentHash != local.ContentHash || string.IsNullOrWhiteSpace(updated.ETag))
            {
                throw new InvalidDataException("Original media update returned a different cloud file revision.");
            }

            return updated;
        }
    }
}
