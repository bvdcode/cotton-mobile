// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonRemoteConflictResolutionService(
        ICottonDeviceToCloudLocalTreeReader localTreeReader,
        CottonRecursiveRemoteContentLoader remoteContentLoader,
        ICottonUploadReceiptStore uploadReceiptStore,
        ICottonFileUploadService uploadService,
        CottonSyncFileUploadSourceFactory sourceFactory,
        CottonSyncProgressHub progressHub,
        TimeProvider timeProvider,
        ILogger<CottonRemoteConflictResolutionService> logger)
    {
        public async Task<int> ReplaceFileConflictsAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);

            try
            {
                CottonSyncDiagnosticLog.ConflictResolutionStarted(logger, root.Id);
                progressHub.Report(CottonSyncProgressSnapshot.ScanningDevice(root.Id));
                CottonDeviceToCloudLocalContentSnapshot localContent = await localTreeReader
                    .ReadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                progressHub.Report(CottonSyncProgressSnapshot.CheckingCloud(root.Id));
                CottonDeviceToCloudRemoteContentSnapshot remoteContent = await remoteContentLoader
                    .LoadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                IReadOnlyList<CottonUploadReceiptSnapshot> uploadReceipts = await uploadReceiptStore
                    .LoadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                    root,
                    localContent,
                    remoteContent,
                    uploadReceipts);
                List<(CottonDeviceToCloudLocalItemSnapshot Local, CottonDeviceToCloudRemoteItemSnapshot Remote)>
                    conflicts = FindFileConflicts(
                    plan,
                    localContent,
                    remoteContent);
                CottonSyncDiagnosticLog.ConflictResolutionPlanned(logger, root.Id, conflicts.Count);
                CottonDeviceToCloudRemoteFolderIndex folders = new(root, remoteContent);
                int replacedCount = 0;
                foreach ((CottonDeviceToCloudLocalItemSnapshot Local, CottonDeviceToCloudRemoteItemSnapshot Remote)
                    conflict in conflicts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Guid operationId = Guid.NewGuid();
                    CottonSyncDiagnosticLog.ConflictResolutionFileStarted(
                        logger,
                        root.Id,
                        conflict.Remote.Entry.Id,
                        operationId);
                    CottonDeviceToCloudSyncPlanItem upload = CottonDeviceToCloudSyncPlanItemFactory
                        .CreateLocal(
                            CottonDeviceToCloudSyncActionKind.UploadNewFile,
                            conflict.Local,
                            conflict.Remote.Entry.Id,
                            conflict.Remote.Entry.ETag)
                        .WithUploadOperationId(operationId);
                    CottonFileUploadSource source = sourceFactory.Create(instanceUri, root, upload);
                    CottonSyncUploadProgressReporter progress = new(
                        root.Id,
                        upload.DisplayName,
                        replacedCount + 1,
                        conflicts.Count,
                        replacedCount,
                        conflicts.Count,
                        upload.SizeBytes,
                        progressHub,
                        timeProvider);
                    CottonUploadReceiptSnapshot pendingReceipt = CottonUploadReceiptSnapshot.CreatePending(
                        upload,
                        operationId,
                        timeProvider.GetUtcNow().UtcDateTime);
                    await uploadReceiptStore.SaveAsync(instanceUri, root, pendingReceipt, cancellationToken)
                        .ConfigureAwait(false);
                    progress.Report(0);
                    CottonFileBrowserEntry updated = await UpdateOrConfirmAsync(
                        instanceUri,
                        root,
                        conflict,
                        folders.ResolveParent(upload),
                        source,
                        progress,
                        cancellationToken).ConfigureAwait(false);
                    ValidateUpdatedFile(conflict.Local, conflict.Remote.Entry.Id, updated);
                    CottonUploadReceiptSnapshot uploadedReceipt = new(
                        conflict.Local.LocalSourceId!,
                        conflict.Local.RelativePath,
                        conflict.Local.LocalUpdatedAtUtc,
                        conflict.Local.SizeBytes,
                        conflict.Local.ContentType,
                        operationId,
                        CottonUploadReceiptStatus.Uploaded,
                        timeProvider.GetUtcNow().UtcDateTime,
                        updated.Id,
                        updated.ETag,
                        conflict.Local.ContentHash);
                    await uploadReceiptStore.SaveAsync(instanceUri, root, uploadedReceipt, cancellationToken)
                        .ConfigureAwait(false);
                    replacedCount++;
                    CottonSyncDiagnosticLog.ConflictResolutionFileCompleted(
                        logger,
                        root.Id,
                        updated.Id,
                        operationId);
                }

                CottonSyncDiagnosticLog.ConflictResolutionCompleted(logger, root.Id, replacedCount);
                return replacedCount;
            }
            finally
            {
                progressHub.Complete(root.Id);
            }
        }

        private static List<(
            CottonDeviceToCloudLocalItemSnapshot Local,
            CottonDeviceToCloudRemoteItemSnapshot Remote)> FindFileConflicts(
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CottonDeviceToCloudLocalContentSnapshot localContent,
            CottonDeviceToCloudRemoteContentSnapshot remoteContent)
        {
            Dictionary<string, CottonDeviceToCloudLocalItemSnapshot> localBySource = localContent.Items
                .Where(item => item.ItemType == CottonFileBrowserEntryType.File
                    && item.LocalSourceId is not null)
                .ToDictionary(item => item.LocalSourceId!, StringComparer.Ordinal);
            Dictionary<string, CottonDeviceToCloudRemoteItemSnapshot> remoteByPath = remoteContent.Items
                .ToDictionary(item => item.RelativePath, StringComparer.OrdinalIgnoreCase);
            List<(CottonDeviceToCloudLocalItemSnapshot Local, CottonDeviceToCloudRemoteItemSnapshot Remote)> conflicts = [];
            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items.Where(item =>
                item.Action == CottonDeviceToCloudSyncActionKind.RemotePathConflict
                && item.TargetType == CottonFileBrowserEntryType.File
                && item.LocalSourceId is not null
                && item.CloudItemId.HasValue
                && !string.IsNullOrWhiteSpace(item.ExpectedRemoteETag)))
            {
                if (!localBySource.TryGetValue(item.LocalSourceId!, out CottonDeviceToCloudLocalItemSnapshot? local)
                    || !string.Equals(local.RelativePath, item.RelativePath, StringComparison.Ordinal)
                    || local.ContentHash != item.ContentHash
                    || !remoteByPath.TryGetValue(item.RelativePath, out CottonDeviceToCloudRemoteItemSnapshot? remote)
                    || remote.Entry.Type != CottonFileBrowserEntryType.File
                    || remote.Entry.Id != item.CloudItemId
                    || !string.Equals(remote.Entry.ETag, item.ExpectedRemoteETag, StringComparison.Ordinal))
                {
                    continue;
                }

                conflicts.Add((local, remote));
            }

            return conflicts;
        }

        private async Task<CottonFileBrowserEntry> UpdateOrConfirmAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            (CottonDeviceToCloudLocalItemSnapshot Local, CottonDeviceToCloudRemoteItemSnapshot Remote) conflict,
            CottonFolderHandle parentFolder,
            CottonFileUploadSource source,
            IProgress<long> progress,
            CancellationToken cancellationToken)
        {
            try
            {
                return await uploadService.UpdateContentAsync(
                        instanceUri,
                        conflict.Remote.Entry.Id,
                        parentFolder,
                        conflict.Remote.Entry.ETag!,
                        source,
                        progress,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                CottonDeviceToCloudRemoteContentSnapshot refreshed = await remoteContentLoader
                    .LoadAsync(instanceUri, root, cancellationToken)
                    .ConfigureAwait(false);
                CottonFileBrowserEntry? updated = refreshed.Items
                    .Where(item => item.RelativePath == conflict.Local.RelativePath)
                    .Select(item => item.Entry)
                    .SingleOrDefault(entry => entry.Id == conflict.Remote.Entry.Id
                        && entry.Type == CottonFileBrowserEntryType.File
                        && entry.Name == conflict.Local.DisplayName
                        && entry.SizeBytes == conflict.Local.SizeBytes
                        && entry.ContentHash == conflict.Local.ContentHash
                        && !string.IsNullOrWhiteSpace(entry.ETag));
                if (updated is not null)
                {
                    CottonSyncDiagnosticLog.ConflictResolutionResponseRecovered(
                        logger,
                        root.Id,
                        updated.Id);
                    return updated;
                }

                throw;
            }
        }

        private static void ValidateUpdatedFile(
            CottonDeviceToCloudLocalItemSnapshot local,
            Guid expectedFileId,
            CottonFileBrowserEntry updated)
        {
            if (updated.Id != expectedFileId
                || updated.Type != CottonFileBrowserEntryType.File
                || updated.Name != local.DisplayName
                || updated.SizeBytes != local.SizeBytes
                || updated.ContentHash != local.ContentHash
                || string.IsNullOrWhiteSpace(updated.ETag))
            {
                throw new InvalidDataException("Cloud conflict update returned a different file revision.");
            }
        }
    }
}
