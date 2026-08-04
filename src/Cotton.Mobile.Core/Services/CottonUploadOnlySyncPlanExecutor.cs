// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUploadOnlySyncPlanExecutor
    {
        private readonly ICottonDeviceToCloudSyncFileOperator _fileOperator;
        private readonly ICottonDeviceToCloudLocalFileOperator _localFileOperator;
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly TimeProvider _timeProvider;

        public CottonUploadOnlySyncPlanExecutor(
            ICottonDeviceToCloudSyncFileOperator fileOperator,
            ICottonDeviceToCloudLocalFileOperator localFileOperator,
            ICottonUploadReceiptStore uploadReceiptStore,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(fileOperator);
            ArgumentNullException.ThrowIfNull(localFileOperator);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);

            _fileOperator = fileOperator;
            _localFileOperator = localFileOperator;
            _uploadReceiptStore = uploadReceiptStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<CottonDeviceToCloudSyncExecutionResult> ExecuteAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CancellationToken cancellationToken = default)
        {
            ValidateInput(instanceUri, root, plan);

            var folderIndex = new CottonDeviceToCloudRemoteFolderIndex(root, plan);
            int uploadedCount = 0;
            int confirmedUploadCount = 0;
            int createdFolderCount = 0;
            int deletedLocalFileCount = 0;
            int skippedCount = 0;
            int blockedCount = 0;

            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (item.Action)
                {
                    case CottonDeviceToCloudSyncActionKind.UploadNewFile:
                        CottonDeviceToCloudLocalFileDeleteStatus? uploadedDeleteStatus =
                            await UploadFileAsync(
                                instanceUri,
                                root,
                                item,
                                folderIndex,
                                cancellationToken).ConfigureAwait(false);
                        uploadedCount++;
                        CountDeleteStatus(
                            uploadedDeleteStatus,
                            ref deletedLocalFileCount,
                            ref skippedCount,
                            ref blockedCount);
                        break;

                    case CottonDeviceToCloudSyncActionKind.ConfirmPendingUpload:
                        CottonDeviceToCloudLocalFileDeleteStatus? confirmedDeleteStatus =
                            await ConfirmUploadAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        confirmedUploadCount++;
                        CountDeleteStatus(
                            confirmedDeleteStatus,
                            ref deletedLocalFileCount,
                            ref skippedCount,
                            ref blockedCount);
                        break;

                    case CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile:
                        CottonDeviceToCloudLocalFileDeleteStatus deleteStatus =
                            await DeleteOriginalAsync(instanceUri, root, item, cancellationToken).ConfigureAwait(false);
                        CountDeleteStatus(
                            deleteStatus,
                            ref deletedLocalFileCount,
                            ref skippedCount,
                            ref blockedCount);
                        break;

                    case CottonDeviceToCloudSyncActionKind.CreateRemoteFolder:
                        await CreateRemoteFolderAsync(
                            instanceUri,
                            root,
                            item,
                            folderIndex,
                            cancellationToken).ConfigureAwait(false);
                        createdFolderCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.KeepExistingFile:
                    case CottonDeviceToCloudSyncActionKind.KeepExistingFolder:
                        skippedCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.RemotePathConflict:
                    case CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision:
                    case CottonDeviceToCloudSyncActionKind.BlockedLocalItemName:
                    case CottonDeviceToCloudSyncActionKind.BlockedLocalSource:
                    case CottonDeviceToCloudSyncActionKind.PendingLocalVersionChanged:
                        blockedCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.UploadChangedFile:
                    case CottonDeviceToCloudSyncActionKind.DeleteRemoteFile:
                    case CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan:
                        throw new InvalidOperationException("Two-way action cannot run through the upload-only executor.");

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plan), "Upload-only sync action is not supported.");
                }
            }

            return new CottonDeviceToCloudSyncExecutionResult(
                uploadedCount,
                confirmedUploadCount,
                refreshedCount: 0,
                createdFolderCount,
                deletedLocalFileCount,
                deletedRemoteFileCount: 0,
                removedManifestCount: 0,
                skippedCount,
                blockedCount);
        }

        private async Task<CottonDeviceToCloudLocalFileDeleteStatus?> UploadFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonDeviceToCloudRemoteFolderIndex folderIndex,
            CancellationToken cancellationToken)
        {
            Guid operationId = item.UploadOperationId ?? Guid.NewGuid();
            CottonDeviceToCloudSyncPlanItem uploadItem = item.WithUploadOperationId(operationId);
            CottonUploadReceiptSnapshot pendingReceipt = CottonUploadReceiptSnapshot.CreatePending(
                uploadItem,
                operationId,
                _timeProvider.GetUtcNow().UtcDateTime);
            if (item.UploadOperationId.HasValue)
            {
                await EnsurePendingReceiptAsync(
                    instanceUri,
                    root,
                    pendingReceipt,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _uploadReceiptStore
                    .SaveAsync(instanceUri, root, pendingReceipt, cancellationToken)
                    .ConfigureAwait(false);
            }

            CottonFolderHandle parentFolder = folderIndex.ResolveParent(uploadItem);
            CottonFileBrowserEntry uploadedFile = await _fileOperator
                .UploadNewFileAsync(instanceUri, root, uploadItem, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            CottonUploadReceiptSnapshot uploadedReceipt = pendingReceipt.MarkUploaded(
                uploadedFile,
                _timeProvider.GetUtcNow().UtcDateTime);
            await _uploadReceiptStore
                .SaveAsync(instanceUri, root, uploadedReceipt, cancellationToken)
                .ConfigureAwait(false);

            return await DeleteOriginalIfEnabledAsync(
                instanceUri,
                root,
                uploadItem,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task EnsurePendingReceiptAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot expected,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonUploadReceiptSnapshot> receipts = await _uploadReceiptStore
                .LoadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonUploadReceiptSnapshot? persisted = receipts.SingleOrDefault(receipt =>
                string.Equals(receipt.LocalSourceId, expected.LocalSourceId, StringComparison.Ordinal));
            if (persisted is null
                || !persisted.IsPending
                || persisted.OperationId != expected.OperationId
                || !string.Equals(persisted.RelativePath, expected.RelativePath, StringComparison.Ordinal)
                || persisted.LocalUpdatedAtUtc != expected.LocalUpdatedAtUtc
                || persisted.SizeBytes != expected.SizeBytes
                || !string.Equals(persisted.ContentType, expected.ContentType, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Upload retry requires its matching pending receipt.");
            }
        }

        private async Task<CottonDeviceToCloudLocalFileDeleteStatus?> ConfirmUploadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            CottonUploadReceiptSnapshot uploadedReceipt =
                CottonUploadReceiptSnapshot.CreateUploadedFromConfirmation(
                    item,
                    _timeProvider.GetUtcNow().UtcDateTime);
            await _uploadReceiptStore
                .SaveAsync(instanceUri, root, uploadedReceipt, cancellationToken)
                .ConfigureAwait(false);

            return await DeleteOriginalIfEnabledAsync(
                instanceUri,
                root,
                item,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task CreateRemoteFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonDeviceToCloudRemoteFolderIndex folderIndex,
            CancellationToken cancellationToken)
        {
            CottonFolderHandle parentFolder = folderIndex.ResolveParent(item);
            CottonFileBrowserEntry createdFolder = await _fileOperator
                .CreateFolderAsync(instanceUri, root, item, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            folderIndex.AddCreatedFolder(item, createdFolder);
        }

        private Task<CottonDeviceToCloudLocalFileDeleteStatus?> DeleteOriginalIfEnabledAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            if (!root.DeletesOriginalsAfterUpload)
            {
                return Task.FromResult<CottonDeviceToCloudLocalFileDeleteStatus?>(null);
            }

            return DeleteOriginalCoreAsync(instanceUri, root, item, cancellationToken);
        }

        private async Task<CottonDeviceToCloudLocalFileDeleteStatus?> DeleteOriginalCoreAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalFileDeleteStatus status = await _localFileOperator
                .DeleteIfUnchangedAsync(instanceUri, root, item, cancellationToken)
                .ConfigureAwait(false);
            return status;
        }

        private Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteOriginalAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken)
        {
            if (!root.DeletesOriginalsAfterUpload || !item.RequiresLocalDelete)
            {
                throw new InvalidOperationException("Local cleanup requires delete-after-upload retention.");
            }

            return _localFileOperator.DeleteIfUnchangedAsync(instanceUri, root, item, cancellationToken);
        }

        private static void CountDeleteStatus(
            CottonDeviceToCloudLocalFileDeleteStatus? status,
            ref int deletedCount,
            ref int skippedCount,
            ref int blockedCount)
        {
            if (!status.HasValue)
            {
                return;
            }

            switch (status.Value)
            {
                case CottonDeviceToCloudLocalFileDeleteStatus.Deleted:
                    deletedCount++;
                    break;

                case CottonDeviceToCloudLocalFileDeleteStatus.AlreadyMissing:
                    skippedCount++;
                    break;

                case CottonDeviceToCloudLocalFileDeleteStatus.Changed:
                case CottonDeviceToCloudLocalFileDeleteStatus.Unsupported:
                    blockedCount++;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), "Local delete status is not supported.");
            }
        }

        private static void ValidateInput(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);
            if (!Uri.Equals(instanceUri, root.InstanceUri))
            {
                throw new ArgumentException("Sync root belongs to a different instance.", nameof(root));
            }

            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new ArgumentException("Upload-only execution requires a device-to-cloud root.", nameof(root));
            }

            if (plan.SyncRootId != root.Id || plan.FolderId != root.CloudFolder.FolderId)
            {
                throw new ArgumentException("Upload-only sync plan does not match the sync root.", nameof(plan));
            }
        }
    }
}
