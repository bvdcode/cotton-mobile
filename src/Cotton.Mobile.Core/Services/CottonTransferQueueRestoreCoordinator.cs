namespace Cotton.Mobile.Services
{
    public class CottonTransferQueueRestoreCoordinator : ICottonTransferQueueRestoreCoordinator
    {
        private const string MissingStagedFileFailure = "Upload file is no longer available on this device.";

        private readonly ICottonTransferMetadataStore _metadataStore;
        private readonly ICottonTransferStagingStore _stagingStore;
        private readonly TimeProvider _timeProvider;

        public CottonTransferQueueRestoreCoordinator(
            ICottonTransferMetadataStore metadataStore,
            ICottonTransferStagingStore stagingStore,
            TimeProvider? timeProvider = null)
        {
            ArgumentNullException.ThrowIfNull(metadataStore);
            ArgumentNullException.ThrowIfNull(stagingStore);

            _metadataStore = metadataStore;
            _stagingStore = stagingStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<IReadOnlyList<CottonTransferQueueItem>> RestoreAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonTransferQueueItem> storedItems =
                await _metadataStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonTransferStagedFileSnapshot> stagedFiles =
                await _stagingStore.ListAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            HashSet<Guid> stagedTransferIds = stagedFiles.Select(file => file.TransferId).ToHashSet();
            DateTime restoredAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            List<CottonTransferQueueItem> restoredItems = storedItems
                .Select(item => RestoreItem(item, stagedTransferIds, restoredAtUtc))
                .ToList();

            await _stagingStore.CleanupAsync(instanceUri, restoredItems, cancellationToken).ConfigureAwait(false);
            await _metadataStore.SaveAsync(instanceUri, restoredItems, cancellationToken).ConfigureAwait(false);
            return restoredItems;
        }

        private static CottonTransferQueueItem RestoreItem(
            CottonTransferQueueItem item,
            HashSet<Guid> stagedTransferIds,
            DateTime restoredAtUtc)
        {
            if (item.IsTerminal || stagedTransferIds.Contains(item.Id))
            {
                return item;
            }

            return item.MarkFailed(MissingStagedFileFailure, restoredAtUtc);
        }
    }
}
