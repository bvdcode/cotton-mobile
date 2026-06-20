namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncCoordinator
    {
        private readonly ICottonCloudToDeviceSyncFolderContentSource _folderContentSource;
        private readonly CottonCloudToDeviceSyncPlanExecutor _planExecutor;
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncRootPauseStore _pauseStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;

        public CottonCloudToDeviceSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncRootPauseStore pauseStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonCloudToDeviceSyncFolderContentSource folderContentSource,
            CottonCloudToDeviceSyncPlanExecutor planExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(pauseStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(folderContentSource);
            ArgumentNullException.ThrowIfNull(planExecutor);

            _rootStore = rootStore;
            _pauseStore = pauseStore;
            _manifestStore = manifestStore;
            _folderContentSource = folderContentSource;
            _planExecutor = planExecutor;
        }

        public async Task<CottonCloudToDeviceSyncRunSummary> RunAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonSyncRootSnapshot> roots =
                await _rootStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlySet<Guid> pausedRootIds =
                await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var results = new List<CottonCloudToDeviceSyncRootRunResult>(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (root.Direction != CottonSyncDirection.CloudToDevice)
                {
                    results.Add(CottonCloudToDeviceSyncRootRunResult.SkippedUnsupportedDirection(root));
                    continue;
                }

                if (pausedRootIds.Contains(root.Id))
                {
                    results.Add(CottonCloudToDeviceSyncRootRunResult.SkippedPaused(root));
                    continue;
                }

                if (!root.CanRunSync)
                {
                    results.Add(CottonCloudToDeviceSyncRootRunResult.SkippedNotReady(root));
                    continue;
                }

                results.Add(await ExecuteRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false));
            }

            return new CottonCloudToDeviceSyncRunSummary(results);
        }

        public async Task<CottonCloudToDeviceSyncRunSummary> RunRootAsync(
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

            CottonCloudToDeviceSyncRootRunResult result;
            if (root.Direction != CottonSyncDirection.CloudToDevice)
            {
                result = CottonCloudToDeviceSyncRootRunResult.SkippedUnsupportedDirection(root);
            }
            else if ((await _pauseStore.LoadPausedRootIdsAsync(instanceUri, cancellationToken).ConfigureAwait(false))
                .Contains(root.Id))
            {
                result = CottonCloudToDeviceSyncRootRunResult.SkippedPaused(root);
            }
            else if (!root.CanRunSync)
            {
                result = CottonCloudToDeviceSyncRootRunResult.SkippedNotReady(root);
            }
            else
            {
                result = await ExecuteRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            }

            return new CottonCloudToDeviceSyncRunSummary([result]);
        }

        private async Task<CottonCloudToDeviceSyncRootRunResult> ExecuteRootAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken)
        {
            CottonFolderContent remoteContent = await _folderContentSource
                .LoadAsync(instanceUri, root.CloudFolder.ToFolderHandle(), cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<CottonSyncedFileSnapshot> localFiles =
                await _manifestStore.LoadAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
            CottonCloudToDeviceSyncPlanSnapshot plan =
                CottonCloudToDeviceSyncPlanner.Create(root, remoteContent, localFiles);
            CottonCloudToDeviceSyncExecutionResult executionResult =
                await _planExecutor.ExecuteAsync(instanceUri, root, plan, cancellationToken).ConfigureAwait(false);

            return CottonCloudToDeviceSyncRootRunResult.Completed(root, plan, executionResult);
        }
    }
}
