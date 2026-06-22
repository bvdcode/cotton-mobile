// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSelectedMediaTransferEnqueueCoordinator :
        ICottonSelectedMediaTransferEnqueueCoordinator
    {
        private readonly ICottonTransferMetadataStore _transferMetadataStore;
        private readonly ICottonTransferStagingStore _transferStagingStore;
        private readonly TimeProvider _timeProvider;
        private readonly Func<Guid> _transferIdFactory;

        public CottonSelectedMediaTransferEnqueueCoordinator(
            ICottonTransferMetadataStore transferMetadataStore,
            ICottonTransferStagingStore transferStagingStore,
            TimeProvider? timeProvider = null,
            Func<Guid>? transferIdFactory = null)
        {
            ArgumentNullException.ThrowIfNull(transferMetadataStore);
            ArgumentNullException.ThrowIfNull(transferStagingStore);

            _transferMetadataStore = transferMetadataStore;
            _transferStagingStore = transferStagingStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _transferIdFactory = transferIdFactory ?? Guid.NewGuid;
        }

        public async Task<CottonSelectedMediaTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CottonFolderHandle destinationFolder,
            string? destinationPath,
            IReadOnlyList<CottonFileUploadSource> sources,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(destinationFolder);
            ArgumentNullException.ThrowIfNull(sources);
            cancellationToken.ThrowIfCancellationRequested();

            CottonTransferDestinationSnapshot destination = CreateTransferDestination(
                destinationFolder,
                destinationPath);
            if (sources.Count == 0)
            {
                return new CottonSelectedMediaTransferEnqueueResult(0, destination, []);
            }

            IReadOnlyList<CottonTransferQueueItem> existingTransfers =
                await _transferMetadataStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var queuedTransfers = new List<CottonTransferQueueItem>(sources.Count);
            DateTime createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (CottonFileUploadSource source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Guid transferId = CreateTransferId();
                await using Stream content = await source.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                CottonTransferStagedFileSnapshot stagedFile =
                    await _transferStagingStore.StageAsync(
                        instanceUri,
                        transferId,
                        source.Snapshot.Name,
                        content,
                        cancellationToken).ConfigureAwait(false);

                queuedTransfers.Add(
                    CottonTransferQueueItem.CreateUpload(
                        transferId,
                        source.Snapshot.Name,
                        stagedFile.SizeBytes,
                        createdAtUtc,
                        destination,
                        source.Snapshot.ContentType,
                        CottonTransferSourceSnapshot.CreateSelectedMedia(source.Snapshot, createdAtUtc)));
            }

            await _transferMetadataStore.SaveAsync(
                instanceUri,
                existingTransfers.Concat(queuedTransfers).ToList(),
                cancellationToken).ConfigureAwait(false);

            return new CottonSelectedMediaTransferEnqueueResult(
                sources.Count,
                destination,
                queuedTransfers);
        }

        private Guid CreateTransferId()
        {
            Guid transferId = _transferIdFactory();
            if (transferId == Guid.Empty)
            {
                throw new InvalidOperationException("Transfer id factory returned an empty id.");
            }

            return transferId;
        }

        private static CottonTransferDestinationSnapshot CreateTransferDestination(
            CottonFolderHandle folder,
            string? destinationPath)
        {
            return new CottonTransferDestinationSnapshot(
                folder.Id,
                folder.Name,
                destinationPath);
        }
    }
}
