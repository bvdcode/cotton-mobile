namespace Cotton.Mobile.Services
{
    public class CottonCloudToDeviceSyncCoordinator
    {
        private readonly ICottonCloudToDeviceSyncFolderContentSource _folderContentSource;
        private readonly CottonCloudToDeviceSyncPlanExecutor _planExecutor;
        private readonly ICottonSyncRootStore _rootStore;
        private readonly ICottonSyncedFileManifestStore _manifestStore;

        public CottonCloudToDeviceSyncCoordinator(
            ICottonSyncRootStore rootStore,
            ICottonSyncedFileManifestStore manifestStore,
            ICottonCloudToDeviceSyncFolderContentSource folderContentSource,
            CottonCloudToDeviceSyncPlanExecutor planExecutor)
        {
            ArgumentNullException.ThrowIfNull(rootStore);
            ArgumentNullException.ThrowIfNull(manifestStore);
            ArgumentNullException.ThrowIfNull(folderContentSource);
            ArgumentNullException.ThrowIfNull(planExecutor);

            _rootStore = rootStore;
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
            var results = new List<CottonCloudToDeviceSyncRootRunResult>(roots.Count);

            foreach (CottonSyncRootSnapshot root in roots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (root.Direction != CottonSyncDirection.CloudToDevice)
                {
                    results.Add(CottonCloudToDeviceSyncRootRunResult.SkippedUnsupportedDirection(root));
                    continue;
                }

                if (!root.CanRunSync)
                {
                    results.Add(CottonCloudToDeviceSyncRootRunResult.SkippedNotReady(root));
                    continue;
                }

                results.Add(await RunRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false));
            }

            return new CottonCloudToDeviceSyncRunSummary(results);
        }

        private async Task<CottonCloudToDeviceSyncRootRunResult> RunRootAsync(
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
