// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonUploadOnlySyncPlanExecutor
    {
        private readonly ICottonDeviceToCloudSyncFileOperator _fileOperator;
        private readonly CottonUploadOnlyFileWorkflow _fileWorkflow;

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
            _fileWorkflow = new CottonUploadOnlyFileWorkflow(
                fileOperator,
                localFileOperator,
                uploadReceiptStore,
                timeProvider ?? TimeProvider.System);
        }

        public async Task<CottonDeviceToCloudSyncExecutionResult> ExecuteAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CancellationToken cancellationToken = default)
        {
            ValidateInput(instanceUri, root, plan);

            CottonDeviceToCloudRemoteFolderIndex folderIndex = new(root, plan);
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
                            await _fileWorkflow.UploadFileAsync(
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
                            await _fileWorkflow
                                .ConfirmUploadAsync(instanceUri, root, item, cancellationToken)
                                .ConfigureAwait(false);
                        confirmedUploadCount++;
                        CountDeleteStatus(
                            confirmedDeleteStatus,
                            ref deletedLocalFileCount,
                            ref skippedCount,
                            ref blockedCount);
                        break;

                    case CottonDeviceToCloudSyncActionKind.DeleteUploadedLocalFile:
                        CottonDeviceToCloudLocalFileDeleteStatus deleteStatus =
                            await _fileWorkflow
                                .DeleteOriginalAsync(instanceUri, root, item, cancellationToken)
                                .ConfigureAwait(false);
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
