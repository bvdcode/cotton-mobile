// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupTransferEnqueueCoordinator :
        ICottonCameraBackupTransferEnqueueCoordinator
    {
        private readonly ICottonCameraBackupPlanningService _planningService;
        private readonly ICottonCameraBackupMediaContentSource _mediaContentSource;
        private readonly ICottonTransferMetadataStore _transferMetadataStore;
        private readonly ICottonTransferStagingStore _transferStagingStore;
        private readonly TimeProvider _timeProvider;
        private readonly Func<Guid> _transferIdFactory;

        public CottonCameraBackupTransferEnqueueCoordinator(
            ICottonCameraBackupPlanningService planningService,
            ICottonCameraBackupMediaContentSource mediaContentSource,
            ICottonTransferMetadataStore transferMetadataStore,
            ICottonTransferStagingStore transferStagingStore,
            TimeProvider? timeProvider = null,
            Func<Guid>? transferIdFactory = null)
        {
            ArgumentNullException.ThrowIfNull(planningService);
            ArgumentNullException.ThrowIfNull(mediaContentSource);
            ArgumentNullException.ThrowIfNull(transferMetadataStore);
            ArgumentNullException.ThrowIfNull(transferStagingStore);

            _planningService = planningService;
            _mediaContentSource = mediaContentSource;
            _transferMetadataStore = transferMetadataStore;
            _transferStagingStore = transferStagingStore;
            _timeProvider = timeProvider ?? TimeProvider.System;
            _transferIdFactory = transferIdFactory ?? Guid.NewGuid;
        }

        public async Task<CottonCameraBackupTransferEnqueueResult> EnqueueAsync(
            Uri instanceUri,
            CottonCameraBackupSettings settings,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(settings);
            cancellationToken.ThrowIfCancellationRequested();

            if (settings.Destination is null)
            {
                return new CottonCameraBackupTransferEnqueueResult(
                    scannedCount: 0,
                    queuedCount: 0,
                    skippedExistingTransferCount: 0,
                    missingStreamCount: 0,
                    missingDestination: true,
                    queuedTransfers: []);
            }

            CottonCameraBackupPlanSnapshot plan =
                await _planningService.PlanAsync(instanceUri, settings, cancellationToken).ConfigureAwait(false);
            IReadOnlyList<CottonTransferQueueItem> existingTransfers =
                await _transferMetadataStore.LoadAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            var existingCameraSources = existingTransfers
                .Where(transfer => transfer.Source?.Kind == CottonTransferSourceKind.CameraBackup
                    && transfer.Status != CottonTransferStatus.Cancelled)
                .Select(transfer => transfer.Source!)
                .ToList();
            var queuedTransfers = new List<CottonTransferQueueItem>();
            int skippedExistingTransferCount = 0;
            int missingStreamCount = 0;
            DateTime createdAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
            CottonTransferDestinationSnapshot destination = CreateTransferDestination(settings.Destination);

            foreach (CottonCameraBackupCandidate candidate in plan.ScanResult.Candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (existingCameraSources.Any(source => source.MatchesCameraBackupIdentity(candidate.Identity)))
                {
                    skippedExistingTransferCount++;
                    continue;
                }

                Guid transferId = CreateTransferId();
                await using Stream? content =
                    await _mediaContentSource.OpenReadAsync(candidate, cancellationToken).ConfigureAwait(false);
                if (content is null)
                {
                    missingStreamCount++;
                    continue;
                }

                CottonTransferStagedFileSnapshot stagedFile =
                    await _transferStagingStore.StageAsync(
                        instanceUri,
                        transferId,
                        candidate.DisplayName,
                        content,
                        cancellationToken).ConfigureAwait(false);
                CottonTransferQueueItem transfer = CottonTransferQueueItem.CreateUpload(
                    transferId,
                    candidate.DisplayName,
                    stagedFile.SizeBytes,
                    createdAtUtc,
                    destination,
                    candidate.ContentType,
                    CottonTransferSourceSnapshot.CreateCameraBackup(candidate));
                queuedTransfers.Add(transfer);
                existingCameraSources.Add(transfer.Source!);
            }

            if (queuedTransfers.Count > 0)
            {
                await _transferMetadataStore.SaveAsync(
                    instanceUri,
                    existingTransfers.Concat(queuedTransfers).ToList(),
                    cancellationToken).ConfigureAwait(false);
            }

            return new CottonCameraBackupTransferEnqueueResult(
                plan.ScanResult.ScannedCount,
                queuedTransfers.Count,
                skippedExistingTransferCount,
                missingStreamCount,
                missingDestination: false,
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
            CottonUploadDestinationSnapshot destination)
        {
            return new CottonTransferDestinationSnapshot(
                destination.FolderId,
                destination.FolderName,
                destination.Path);
        }
    }
}
