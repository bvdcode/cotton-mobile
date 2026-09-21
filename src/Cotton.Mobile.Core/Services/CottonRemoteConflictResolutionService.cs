// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonRemoteConflictResolutionService(
        ICottonDeviceToCloudLocalTreeReader localTreeReader,
        CottonRecursiveRemoteContentLoader remoteContentLoader,
        ICottonUploadReceiptStore uploadReceiptStore,
        FileSystemCottonSyncReviewStore reviewStore,
        CottonCloudFileReplacement replacement,
        CottonSyncRootExecutionLock executionLock,
        CottonSyncProgressHub progressHub,
        TimeProvider timeProvider,
        ILogger<CottonRemoteConflictResolutionService> logger)
    {
        public Task<CottonSyncReviewState> LoadAsync(CottonSyncRootSnapshot root, CancellationToken cancellationToken)
        {
            return reviewStore.LoadAsync(root, cancellationToken);
        }

        public Task<CottonSyncReviewState> ScanAsync(
            CottonSyncRootSnapshot root, CancellationToken cancellationToken = default)
        {
            return executionLock.ExecuteAsync(root, async token =>
            {
                await reviewStore.InvalidateAsync(root, token).ConfigureAwait(false);
                CottonDeviceToCloudLocalContentSnapshot local = await localTreeReader
                    .ReadAsync(root.InstanceUri, root, token).ConfigureAwait(false);
                CottonDeviceToCloudRemoteContentSnapshot remote = await remoteContentLoader
                    .LoadAsync(root.InstanceUri, root, token).ConfigureAwait(false);
                IReadOnlyList<CottonUploadReceiptSnapshot> receipts = await uploadReceiptStore
                    .LoadAsync(root.InstanceUri, root, token).ConfigureAwait(false);
                CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(root, local, remote, receipts);
                return await CaptureAsync(root, local, remote, plan, token).ConfigureAwait(false);
            }, cancellationToken);
        }

        public Task ApproveAsync(
            CottonSyncRootSnapshot root,
            IReadOnlyList<CottonSyncConflictSnapshot> selected,
            CancellationToken cancellationToken = default)
        {
            return executionLock.ExecuteAsync(root, async token =>
            {
                await reviewStore.UpdateAsync(root, state =>
                {
                    Dictionary<string, CottonSyncConflictSnapshot> currentConflicts = state.Conflicts
                        .ToDictionary(item => item.LocalFile.LocalSourceId!, StringComparer.Ordinal);
                    Dictionary<string, CottonSyncReplacementApproval> approvals = state.Approvals
                        .ToDictionary(item => item.Conflict.LocalFile.LocalSourceId!, StringComparer.Ordinal);
                    foreach (CottonSyncConflictSnapshot item in selected)
                    {
                        if (!item.CanReplace
                            || !currentConflicts.TryGetValue(item.LocalFile.LocalSourceId!, out CottonSyncConflictSnapshot? current)
                            || !current.Matches(item))
                        {
                            throw new InvalidOperationException("The selected conflict has changed. Refresh the comparison.");
                        }

                        string source = item.LocalFile.LocalSourceId!;
                        if (approvals.TryGetValue(source, out CottonSyncReplacementApproval? approval)
                            && approval.Conflict.Matches(item))
                        {
                            continue;
                        }

                        approvals[source] = new CottonSyncReplacementApproval(item, Guid.NewGuid());
                    }

                    return new CottonSyncReviewState(root.StableKey, null, 0, state.Conflicts, [.. approvals.Values], state.VerifiedFiles);
                }, token).ConfigureAwait(false);
                return true;
            }, cancellationToken);
        }

        public Task<CottonSyncReviewState> CaptureAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot local,
            CottonDeviceToCloudRemoteContentSnapshot remote,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CancellationToken cancellationToken)
        {
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localBySource = local.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File && item.LocalSourceId is not null)
                .ToDictionary(item => item.LocalSourceId!, StringComparer.Ordinal);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = remote.Items
                .ToDictionary(item => item.RelativePath, StringComparer.OrdinalIgnoreCase);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById = remote.Items.ToDictionary(item => item.Entry.Id);
            List<CottonSyncConflictSnapshot> conflicts = [];
            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items.Where(item => item.IsBlocked))
            {
                if (item.LocalSourceId is null || !localBySource.TryGetValue(item.LocalSourceId, out CottonDeviceToCloudLocalItemSnapshot? file))
                {
                    continue;
                }

                if (!remoteByPath.TryGetValue(file.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? cloud)
                    && (!item.CloudItemId.HasValue || !remoteById.TryGetValue(item.CloudItemId.Value, out cloud)))
                {
                    continue;
                }

                if (cloud.Entry.Type == CottonFileBrowserEntryType.File && file.ContentHash == cloud.Entry.ContentHash
                    && file.SizeBytes == cloud.Entry.SizeBytes)
                {
                    continue;
                }

                conflicts.Add(new CottonSyncConflictSnapshot(file, cloud.Entry.Id, cloud.Entry.Type,
                    cloud.Entry.SizeBytes, cloud.Entry.ContentHash, cloud.Entry.ETag, cloud.Entry.UpdatedAtUtc, cloud.RelativePath));
            }

            return reviewStore.UpdateAsync(root, state => new CottonSyncReviewState(
                root.StableKey, null, 0, conflicts, state.Approvals, state.VerifiedFiles), cancellationToken);
        }

        public async Task<int> ApplyAsync(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudLocalContentSnapshot local,
            CottonDeviceToCloudRemoteContentSnapshot remote,
            CancellationToken cancellationToken)
        {
            CottonSyncReviewState state = await reviewStore.LoadAsync(root, cancellationToken).ConfigureAwait(false);
            if (state.Approvals.Count == 0)
            {
                return 0;
            }

            IReadOnlyList<CottonUploadReceiptSnapshot> receipts = await uploadReceiptStore
                .LoadAsync(root.InstanceUri, root, cancellationToken).ConfigureAwait(false);
            Dictionary<string, CottonUploadReceiptSnapshot> receiptsBySource = receipts
                .ToDictionary(item => item.LocalSourceId, StringComparer.Ordinal);
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localBySource = local.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File && item.LocalSourceId is not null)
                .ToDictionary(item => item.LocalSourceId!, StringComparer.Ordinal);
            Dictionary<Guid, CottonDeviceToCloudRemoteItemSnapshot> remoteById = remote.Items.ToDictionary(item => item.Entry.Id);
            CottonDeviceToCloudRemoteFolderIndex folders = new(root, remote);
            int completed = 0;
            CottonSyncDiagnosticLog.ConflictResolutionPlanned(logger, root.Id, state.Approvals.Count);
            foreach (CottonSyncReplacementApproval approval in state.Approvals)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CottonSyncConflictSnapshot conflict = approval.Conflict;
                bool applied = false;
                if (localBySource.TryGetValue(conflict.LocalFile.LocalSourceId!, out CottonDeviceToCloudLocalItemSnapshot? file)
                    && conflict.MatchesLocal(file)
                    && remoteById.TryGetValue(conflict.RemoteFileId, out CottonDeviceToCloudRemoteItemSnapshot? cloud)
                    && cloud.RelativePath == file.RelativePath && cloud.Entry.Type == CottonFileBrowserEntryType.File
                    && (cloud.Entry.ContentHash == file.ContentHash && cloud.Entry.SizeBytes == file.SizeBytes
                        || cloud.Entry.ETag == conflict.RemoteETag && cloud.Entry.ContentHash == conflict.RemoteContentHash
                            && cloud.Entry.SizeBytes == conflict.RemoteSizeBytes))
                {
                    bool alreadyConfirmed = receiptsBySource.TryGetValue(file.LocalSourceId!, out CottonUploadReceiptSnapshot? receipt)
                        && receipt.IsUploaded && receipt.RelativePath == file.RelativePath
                        && receipt.ContentHash == file.ContentHash && receipt.SizeBytes == file.SizeBytes
                        && cloud.Entry.ContentHash == file.ContentHash && cloud.Entry.SizeBytes == file.SizeBytes
                        && receipt.RemoteFileId == cloud.Entry.Id && receipt.RemoteETag == cloud.Entry.ETag;
                    if (!alreadyConfirmed)
                    {
                        CottonSyncUploadProgressReporter progress = new(root.Id, file.DisplayName,
                            completed + 1, state.Approvals.Count, completed, state.Approvals.Count,
                            file.SizeBytes, progressHub, timeProvider);
                        progress.Report(0);
                        CottonSyncDiagnosticLog.ConflictResolutionFileStarted(logger, root.Id, cloud.Entry.Id, approval.OperationId);
                        await replacement.ExecuteAsync(root, file, cloud.Entry, folders.ResolveParent(conflict.ToPlanItem()),
                            conflict.RemoteETag!, approval.OperationId, progress, cancellationToken).ConfigureAwait(false);
                        completed++;
                    }
                    applied = true;
                    CottonSyncDiagnosticLog.ConflictResolutionFileCompleted(logger, root.Id, cloud.Entry.Id, approval.OperationId);
                }
                else
                {
                    CottonSyncDiagnosticLog.ReplacementChanged(logger, root.Id);
                }

                await reviewStore.UpdateAsync(root, current => new CottonSyncReviewState(root.StableKey,
                    null, 0, applied
                        ? [.. current.Conflicts.Where(item => item.LocalFile.LocalSourceId != conflict.LocalFile.LocalSourceId)]
                        : current.Conflicts,
                    [.. current.Approvals.Where(item => item.OperationId != approval.OperationId)],
                    current.VerifiedFiles), cancellationToken).ConfigureAwait(false);
            }

            return completed;
        }
    }
}
