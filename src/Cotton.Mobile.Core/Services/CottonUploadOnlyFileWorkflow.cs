// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonUploadOnlyFileWorkflow
    {
        private readonly ICottonDeviceToCloudSyncFileOperator _fileOperator;
        private readonly ICottonDeviceToCloudLocalFileOperator _localFileOperator;
        private readonly ICottonUploadReceiptStore _uploadReceiptStore;
        private readonly TimeProvider _timeProvider;

        public CottonUploadOnlyFileWorkflow(
            ICottonDeviceToCloudSyncFileOperator fileOperator,
            ICottonDeviceToCloudLocalFileOperator localFileOperator,
            ICottonUploadReceiptStore uploadReceiptStore,
            TimeProvider timeProvider)
        {
            _fileOperator = fileOperator;
            _localFileOperator = localFileOperator;
            _uploadReceiptStore = uploadReceiptStore;
            _timeProvider = timeProvider;
        }

        public async Task<CottonDeviceToCloudLocalFileDeleteStatus?> UploadFileAsync(
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

        public async Task<CottonDeviceToCloudLocalFileDeleteStatus?> ConfirmUploadAsync(
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

        public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteOriginalAsync(
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
                || !string.Equals(persisted.ContentType, expected.ContentType, StringComparison.Ordinal)
                || !string.Equals(persisted.ContentHash, expected.ContentHash, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Upload retry requires its matching pending receipt.");
            }
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
    }
}
