// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonUploadOnlySyncPlanExecutor
    {
        private readonly ICottonDeviceToCloudSyncFileOperator _fileOperator;
        private readonly CottonUploadOnlyFileWorkflow _fileWorkflow;
        private readonly CottonSyncProgressHub _progressHub;
        private readonly TimeProvider _timeProvider;
        private readonly ILogger<CottonUploadOnlySyncPlanExecutor> _logger;

        public CottonUploadOnlySyncPlanExecutor(
            ICottonDeviceToCloudSyncFileOperator fileOperator,
            ICottonDeviceToCloudLocalFileOperator localFileOperator,
            ICottonUploadReceiptStore uploadReceiptStore,
            CottonSyncProgressHub progressHub,
            ILogger<CottonUploadOnlySyncPlanExecutor> logger,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(fileOperator);
            ArgumentNullException.ThrowIfNull(localFileOperator);
            ArgumentNullException.ThrowIfNull(uploadReceiptStore);
            ArgumentNullException.ThrowIfNull(progressHub);
            ArgumentNullException.ThrowIfNull(logger);

            _fileOperator = fileOperator;
            _progressHub = progressHub;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _logger = logger;
            _fileWorkflow = new CottonUploadOnlyFileWorkflow(
                fileOperator,
                localFileOperator,
                uploadReceiptStore,
                _timeProvider);
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
            int totalChangeCount = plan.ExecutableChangeCount;
            int completedChangeCount = 0;
            int uploadNumber = 0;
            ReportProgress(root.Id, completedChangeCount, totalChangeCount);

            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (item.Action)
                {
                    case CottonDeviceToCloudSyncActionKind.UploadNewFile:
                        uploadNumber++;
                        long uploadSizeBytes = item.SizeBytes
                            ?? throw new InvalidDataException("Upload plan item size is required.");
                        CottonSyncDiagnosticLog.UploadStarted(
                            _logger,
                            root.Id,
                            uploadNumber,
                            uploadSizeBytes);
                        CottonSyncUploadProgressReporter uploadProgress = new(
                            root.Id,
                            item.DisplayName,
                            completedChangeCount,
                            totalChangeCount,
                            uploadSizeBytes,
                            _progressHub,
                            _timeProvider);
                        uploadProgress.Report(0);
                        CottonDeviceToCloudLocalFileDeleteStatus? uploadedDeleteStatus =
                            await _fileWorkflow.UploadFileAsync(
                                instanceUri,
                                root,
                                item,
                                folderIndex,
                                uploadProgress,
                                cancellationToken).ConfigureAwait(false);
                        CottonSyncDiagnosticLog.UploadCompleted(_logger, root.Id, uploadNumber);
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

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plan), "Upload-only sync action is not supported.");
                }

                if (item.RequiresServerMutation || item.RequiresLocalMutation)
                {
                    completedChangeCount++;
                    ReportProgress(root.Id, completedChangeCount, totalChangeCount);
                }
            }

            return new CottonDeviceToCloudSyncExecutionResult(
                uploadedCount,
                confirmedUploadCount,
                createdFolderCount,
                deletedLocalFileCount,
                skippedCount,
                blockedCount);
        }

        private void ReportProgress(Guid rootId, int completedItemCount, int totalItemCount)
        {
            _progressHub.Report(CottonSyncProgressSnapshot.ApplyingChanges(
                rootId,
                completedItemCount,
                totalItemCount));
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
