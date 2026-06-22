// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Text;

namespace Cotton.Mobile.Services
{
    public class CottonShareTransferEnqueueCoordinator : ICottonShareTransferEnqueueCoordinator
    {
        private readonly ICottonShareIntakeStore _shareIntakeStore;
        private readonly ICottonShareContentStagingStore _shareContentStagingStore;
        private readonly ICottonTransferMetadataStore _transferMetadataStore;
        private readonly ICottonTransferStagingStore _transferStagingStore;
        private readonly TimeProvider _timeProvider;
        private readonly Func<Guid> _transferIdFactory;

        public CottonShareTransferEnqueueCoordinator(
            ICottonShareIntakeStore shareIntakeStore,
            ICottonShareContentStagingStore shareContentStagingStore,
            ICottonTransferMetadataStore transferMetadataStore,
            ICottonTransferStagingStore transferStagingStore,
            TimeProvider? timeProvider = null,
            Func<Guid>? transferIdFactory = null)
        {
            ArgumentNullException.ThrowIfNull(shareIntakeStore);
            ArgumentNullException.ThrowIfNull(shareContentStagingStore);
            ArgumentNullException.ThrowIfNull(transferMetadataStore);
            ArgumentNullException.ThrowIfNull(transferStagingStore);

            _shareIntakeStore = shareIntakeStore;
            _shareContentStagingStore = shareContentStagingStore;
            _transferMetadataStore = transferMetadataStore;
            _transferStagingStore = transferStagingStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _transferIdFactory = transferIdFactory ?? Guid.NewGuid;
        }

        public async Task<CottonShareTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);

            IReadOnlyList<CottonShareIntakeSnapshot> inboxSnapshots =
                await _shareIntakeStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            List<CaptureEnqueueCandidate> candidates = inboxSnapshots
                .SelectMany(CreateCandidates)
                .ToList();
            if (candidates.Count == 0)
            {
                return new CottonShareTransferEnqueueResult(
                    0,
                    inboxSnapshots.Sum(snapshot => snapshot.Items.Count),
                    []);
            }

            IReadOnlyList<CottonTransferQueueItem> existingTransfers =
                await _transferMetadataStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var queuedTransfers = new List<CottonTransferQueueItem>(candidates.Count);
            DateTime createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

            foreach (CaptureEnqueueCandidate candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Guid transferId = CreateTransferId();
                await using Stream content = CreateUploadContentStream(candidate.Item);
                string uploadDisplayName = CreateUploadDisplayName(candidate.Item);
                CottonTransferStagedFileSnapshot stagedFile =
                    await _transferStagingStore.StageAsync(
                        instanceUri,
                        transferId,
                        uploadDisplayName,
                        content,
                        cancellationToken).ConfigureAwait(false);
                queuedTransfers.Add(
                    CottonTransferQueueItem.CreateUpload(
                        transferId,
                        uploadDisplayName,
                        stagedFile.SizeBytes,
                        createdAtUtc,
                        new CottonTransferDestinationSnapshot(
                            candidate.Snapshot.Destination!.FolderId,
                            candidate.Snapshot.Destination.FolderName,
                            candidate.Snapshot.Destination.Path),
                        CreateContentType(candidate),
                        CottonTransferSourceSnapshot.CreateShareInbox(
                            candidate.Item.Id,
                            candidate.Snapshot.ReceivedAtUtc,
                            stagedFile.SizeBytes)));
            }

            IReadOnlySet<Guid> enqueuedItemIds = candidates
                .Select(candidate => candidate.Item.Id)
                .ToHashSet();
            List<CottonShareIntakeSnapshot> remainingInbox = RemoveEnqueuedItems(
                inboxSnapshots,
                enqueuedItemIds);
            await _transferMetadataStore.SaveAsync(
                instanceUri,
                existingTransfers.Concat(queuedTransfers).ToList(),
                cancellationToken).ConfigureAwait(false);
            await _shareIntakeStore.SaveAsync(remainingInbox, cancellationToken).ConfigureAwait(false);
            await _shareContentStagingStore.CleanupAsync(remainingInbox, cancellationToken).ConfigureAwait(false);

            return new CottonShareTransferEnqueueResult(
                queuedTransfers.Count,
                remainingInbox.Sum(snapshot => snapshot.Items.Count),
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

        private static IEnumerable<CaptureEnqueueCandidate> CreateCandidates(
            CottonShareIntakeSnapshot snapshot)
        {
            if (snapshot.Status != CottonShareIntakeStatus.Pending || snapshot.Destination is null)
            {
                yield break;
            }

            foreach (CottonShareIntakeItemSnapshot item in snapshot.Items)
            {
                if (item.Type == CottonShareIntakeItemType.Text)
                {
                    yield return new CaptureEnqueueCandidate(snapshot, item);
                }
                else if (item.Type == CottonShareIntakeItemType.Uri
                    && item.HasStagedContent
                    && File.Exists(item.StagedPath))
                {
                    yield return new CaptureEnqueueCandidate(snapshot, item);
                }
            }
        }

        private static List<CottonShareIntakeSnapshot> RemoveEnqueuedItems(
            IEnumerable<CottonShareIntakeSnapshot> snapshots,
            IReadOnlySet<Guid> enqueuedItemIds)
        {
            var remaining = new List<CottonShareIntakeSnapshot>();
            foreach (CottonShareIntakeSnapshot snapshot in snapshots)
            {
                List<CottonShareIntakeItemSnapshot> remainingItems = snapshot.Items
                    .Where(item => !enqueuedItemIds.Contains(item.Id))
                    .ToList();
                if (remainingItems.Count == 0)
                {
                    continue;
                }

                remaining.Add(
                    new CottonShareIntakeSnapshot(
                        snapshot.Id,
                        snapshot.Kind,
                        snapshot.Status,
                        snapshot.SourceMimeType,
                        remainingItems,
                        snapshot.FailureMessage,
                        snapshot.ReceivedAtUtc,
                        snapshot.Destination));
            }

            return remaining;
        }

        private static Stream CreateUploadContentStream(CottonShareIntakeItemSnapshot item)
        {
            if (item.Type == CottonShareIntakeItemType.Text)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(item.Value));
            }

            return new FileStream(
                item.StagedPath!,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
        }

        private static string CreateUploadDisplayName(CottonShareIntakeItemSnapshot item)
        {
            return CottonShareCaptureUploadName.Create(item);
        }

        private static string? CreateContentType(CaptureEnqueueCandidate candidate)
        {
            return candidate.Item.Type == CottonShareIntakeItemType.Text
                ? CottonShareTextUploadName.TextContentType
                : candidate.Item.MimeType ?? candidate.Snapshot.SourceMimeType;
        }

        private sealed class CaptureEnqueueCandidate
        {
            public CaptureEnqueueCandidate(
                CottonShareIntakeSnapshot snapshot,
                CottonShareIntakeItemSnapshot item)
            {
                Snapshot = snapshot;
                Item = item;
            }

            public CottonShareIntakeSnapshot Snapshot { get; }

            public CottonShareIntakeItemSnapshot Item { get; }
        }
    }
}
