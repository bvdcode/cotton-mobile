namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncCoordinator
    {
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;
        private readonly ICottonDeviceToCloudLocalTreeReader _localTreeReader;
        private readonly ICottonDeviceToCloudRemoteFolderContentSource _remoteFolderContentSource;
        private readonly CottonDeviceToCloudSyncPlanExecutor _planExecutor;

        public CottonDeviceToCloudSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonDeviceToCloudLocalTreeReader localTreeReader,
            ICottonDeviceToCloudRemoteFolderContentSource remoteFolderContentSource,
            CottonDeviceToCloudSyncPlanExecutor planExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(localTreeReader);
            ArgumentNullException.ThrowIfNull(remoteFolderContentSource);
            ArgumentNullException.ThrowIfNull(planExecutor);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _localTreeReader = localTreeReader;
            _remoteFolderContentSource = remoteFolderContentSource;
            _planExecutor = planExecutor;
        }

        public async Task<CottonDeviceToCloudSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var results = new List<CottonDeviceToCloudSyncRootRunResult>(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken)
                    .ConfigureAwait(false));
            }

            return new CottonDeviceToCloudSyncRunSummary(results);
        }

        public async Task<CottonDeviceToCloudSyncRunSummary> RunRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(root);
            if (!Uri.Equals(root.InstanceUri, instanceUri))
            {
                throw new ArgumentException("Sync root belongs to a different instance.", nameof(root));
            }

            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            CottonDeviceToCloudSyncRootRunResult result =
                await RunRootCoreAsync(instanceUri, root, pausedRootIds, cancellationToken).ConfigureAwait(false);
            return new CottonDeviceToCloudSyncRunSummary([result]);
        }

        private async Task<CottonDeviceToCloudSyncRootRunResult> RunRootCoreAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            IReadOnlySet<Guid> pausedRootIds,
            CancellationToken cancellationToken)
        {
            if (root.Direction == CottonSyncDirection.CloudToDevice)
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedDirection(root);
            }

            if (pausedRootIds.Contains(root.Id))
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedPaused(root);
            }

            if (CottonDeviceToCloudSyncRootCapability.HasUnsupportedLocalRoot(root))
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedUnsupportedLocalRoot(root);
            }

            if (!root.CanRunSync)
            {
                return CottonDeviceToCloudSyncRootRunResult.SkippedNotReady(root);
            }

            return await ExecuteRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
        }

        private async Task<CottonDeviceToCloudSyncRootRunResult> ExecuteRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            CottonDeviceToCloudLocalContentSnapshot localContent = await _localTreeReader
                .ReadAsync(instanceUri, root, cancellationToken)
                .ConfigureAwait(false);
            CottonDeviceToCloudRemoteContentSnapshot remoteContent =
                await LoadRecursiveRemoteContentAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonSyncedFileSnapshot> manifestFiles =
                await _manifestStore.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonDeviceToCloudSyncPlanSnapshot plan =
                CottonDeviceToCloudSyncPlanner.Create(root, localContent, remoteContent, manifestFiles);
            CottonDeviceToCloudSyncExecutionResult executionResult =
                await _planExecutor.ExecuteAsync(instanceUri, root, plan, cancellationToken).ConfigureAwait(false);

            return CottonDeviceToCloudSyncRootRunResult.Completed(root, plan, executionResult);
        }

        private async Task<CottonDeviceToCloudRemoteContentSnapshot> LoadRecursiveRemoteContentAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            var items = new List<CottonDeviceToCloudRemoteItemSnapshot>();
            var folders = new Queue<(CottonFolderHandle Folder, string RelativePath)>();
            var visitedFolderIds = new HashSet<Guid>();

            folders.Enqueue((root.CloudFolder.ToFolderHandle(), string.Empty));
            while (folders.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                (CottonFolderHandle folder, string folderRelativePath) = folders.Dequeue();
                if (!visitedFolderIds.Add(folder.Id))
                {
                    continue;
                }

                CottonFolderContent content = await _remoteFolderContentSource
                    .LoadAsync(instanceUri, folder, cancellationToken)
                    .ConfigureAwait(false);
                foreach (CottonFileBrowserEntry entry in content.Entries)
                {
                    string relativePath = entry.Type == CottonFileBrowserEntryType.Folder
                        ? CottonSyncRelativePath.CreateChildFolderPath(folderRelativePath, entry.Name)
                        : CottonSyncRelativePath.CreateFilePath(folderRelativePath, entry.Name);
                    items.Add(new CottonDeviceToCloudRemoteItemSnapshot(entry, relativePath));
                    if (entry.Type == CottonFileBrowserEntryType.Folder)
                    {
                        folders.Enqueue((new CottonFolderHandle(entry.Id, entry.Name), relativePath));
                    }
                }
            }

            return new CottonDeviceToCloudRemoteContentSnapshot(
                root.CloudFolder.FolderId,
                root.CloudFolder.FolderName,
                items);
        }
    }
}
