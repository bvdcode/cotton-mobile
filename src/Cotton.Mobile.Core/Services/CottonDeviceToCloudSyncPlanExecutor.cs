// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncPlanExecutor
    {
        private const char RelativePathSeparator = '/';

        private readonly ICottonDeviceToCloudSyncFileOperator _fileOperator;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly TimeProvider _timeProvider;

        public CottonDeviceToCloudSyncPlanExecutor(
            ICottonDeviceToCloudSyncFileOperator fileOperator,
            ICottonSyncedFileManifestStore manifestStore,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(fileOperator);
            ArgumentNullException.ThrowIfNull(manifestStore);

            _fileOperator = fileOperator;
            _manifestStore = manifestStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<CottonDeviceToCloudSyncExecutionResult> ExecuteAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            ArgumentNullException.ThrowIfNull(plan);

            if (plan.SyncRootId != root.Id || plan.FolderId != root.CloudFolder.FolderId)
            {
                throw new ArgumentException("Device-to-cloud sync plan does not match the sync root.", nameof(plan));
            }

            Dictionary<string, CottonFolderHandle> foldersByPath = CreateInitialFolderMap(root, plan);
            int uploadedCount = 0;
            int refreshedCount = 0;
            int createdFolderCount = 0;
            int deletedRemoteFileCount = 0;
            int removedManifestCount = 0;
            int skippedCount = 0;
            int blockedCount = 0;

            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                switch (item.Action)
                {
                    case CottonDeviceToCloudSyncActionKind.UploadNewFile:
                        await UploadNewFileAsync(instanceUri, root, item, foldersByPath, cancellationToken)
                            .ConfigureAwait(false);
                        uploadedCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.UploadChangedFile:
                        await UploadChangedFileAsync(instanceUri, root, item, foldersByPath, cancellationToken)
                            .ConfigureAwait(false);
                        refreshedCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.CreateRemoteFolder:
                        await CreateRemoteFolderAsync(instanceUri, root, item, foldersByPath, cancellationToken)
                            .ConfigureAwait(false);
                        createdFolderCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.DeleteRemoteFile:
                        await _fileOperator.DeleteRemoteFileAsync(instanceUri, root, item, cancellationToken)
                            .ConfigureAwait(false);
                        await _manifestStore
                            .RemoveAsync(instanceUri, root, GetRequiredCloudItemId(item), cancellationToken)
                            .ConfigureAwait(false);
                        deletedRemoteFileCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.RemoveManifestOrphan:
                        await _manifestStore
                            .RemoveAsync(instanceUri, root, GetRequiredCloudItemId(item), cancellationToken)
                            .ConfigureAwait(false);
                        removedManifestCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.KeepExistingFile:
                    case CottonDeviceToCloudSyncActionKind.KeepExistingFolder:
                        skippedCount++;
                        break;

                    case CottonDeviceToCloudSyncActionKind.RemotePathConflict:
                    case CottonDeviceToCloudSyncActionKind.RemoteRevisionChanged:
                    case CottonDeviceToCloudSyncActionKind.RemoteTargetMissing:
                    case CottonDeviceToCloudSyncActionKind.NeedsFreshServerRevision:
                    case CottonDeviceToCloudSyncActionKind.BlockedLocalItemName:
                        blockedCount++;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(plan), "Device-to-cloud sync action is not supported.");
                }
            }

            return new CottonDeviceToCloudSyncExecutionResult(
                uploadedCount,
                refreshedCount,
                createdFolderCount,
                deletedRemoteFileCount,
                removedManifestCount,
                skippedCount,
                blockedCount);
        }

        private async Task UploadNewFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            IReadOnlyDictionary<string, CottonFolderHandle> foldersByPath,
            CancellationToken cancellationToken)
        {
            CottonFolderHandle parentFolder = ResolveParentFolder(item, foldersByPath);
            CottonFileBrowserEntry uploadedFile = await _fileOperator
                .UploadNewFileAsync(instanceUri, root, item, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            await SaveManifestItemAsync(instanceUri, root, item, uploadedFile, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task UploadChangedFileAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            IReadOnlyDictionary<string, CottonFolderHandle> foldersByPath,
            CancellationToken cancellationToken)
        {
            CottonFolderHandle parentFolder = ResolveParentFolder(item, foldersByPath);
            CottonFileBrowserEntry uploadedFile = await _fileOperator
                .UploadChangedFileAsync(instanceUri, root, item, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            await SaveManifestItemAsync(instanceUri, root, item, uploadedFile, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task CreateRemoteFolderAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            Dictionary<string, CottonFolderHandle> foldersByPath,
            CancellationToken cancellationToken)
        {
            CottonFolderHandle parentFolder = ResolveParentFolder(item, foldersByPath);
            CottonFileBrowserEntry createdFolder = await _fileOperator
                .CreateFolderAsync(instanceUri, root, item, parentFolder, cancellationToken)
                .ConfigureAwait(false);
            if (createdFolder.Type != CottonFileBrowserEntryType.Folder)
            {
                throw new InvalidOperationException("Device-to-cloud folder creation returned a non-folder item.");
            }

            if (!string.Equals(createdFolder.Name, item.DisplayName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Device-to-cloud folder creation returned a different folder name.");
            }

            foldersByPath[item.RelativePath] = new CottonFolderHandle(createdFolder.Id, createdFolder.Name);
        }

        private async Task SaveManifestItemAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CottonFileBrowserEntry uploadedFile,
            CancellationToken cancellationToken)
        {
            DateTime syncedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            await _manifestStore
                .AddOrReplaceAsync(
                    instanceUri,
                    root,
                    item.CreateManifestItem(uploadedFile, syncedAtUtc),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static Dictionary<string, CottonFolderHandle> CreateInitialFolderMap(
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanSnapshot plan)
        {
            var foldersByPath = new Dictionary<string, CottonFolderHandle>(StringComparer.OrdinalIgnoreCase)
            {
                [string.Empty] = root.CloudFolder.ToFolderHandle(),
            };

            foreach (CottonDeviceToCloudSyncPlanItem item in plan.Items)
            {
                if (item.Action != CottonDeviceToCloudSyncActionKind.KeepExistingFolder
                    || item.TargetType != CottonFileBrowserEntryType.Folder
                    || !item.CloudItemId.HasValue)
                {
                    continue;
                }

                foldersByPath[item.RelativePath] = new CottonFolderHandle(
                    item.CloudItemId.Value,
                    item.DisplayName);
            }

            return foldersByPath;
        }

        private static CottonFolderHandle ResolveParentFolder(
            CottonDeviceToCloudSyncPlanItem item,
            IReadOnlyDictionary<string, CottonFolderHandle> foldersByPath)
        {
            string parentPath = GetParentPath(item.RelativePath);
            if (foldersByPath.TryGetValue(parentPath, out CottonFolderHandle? parentFolder))
            {
                return parentFolder;
            }

            throw new InvalidOperationException("Device-to-cloud sync parent folder is not available.");
        }

        private static string GetParentPath(string relativePath)
        {
            string normalizedPath = CottonSyncRelativePath.NormalizeFilePath(relativePath, nameof(relativePath));
            int separatorIndex = normalizedPath.LastIndexOf(RelativePathSeparator);
            return separatorIndex < 0 ? string.Empty : normalizedPath[..separatorIndex];
        }

        private static Guid GetRequiredCloudItemId(CottonDeviceToCloudSyncPlanItem item)
        {
            return item.CloudItemId
                ?? throw new InvalidOperationException("Device-to-cloud sync item requires a cloud item id.");
        }
    }
}
