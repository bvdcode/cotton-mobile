// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public sealed class CottonAndroidBackgroundTransferJobRunner : ICottonAndroidBackgroundTransferJobRunner
    {
        private readonly ICottonQueuedUploadExecutor _queuedUploadExecutor;
        private readonly ICottonTransferQueueRestoreCoordinator _restoreCoordinator;

        public CottonAndroidBackgroundTransferJobRunner(
            ICottonQueuedUploadExecutor queuedUploadExecutor,
            ICottonTransferQueueRestoreCoordinator restoreCoordinator)
        {
            ArgumentNullException.ThrowIfNull(queuedUploadExecutor);
            ArgumentNullException.ThrowIfNull(restoreCoordinator);

            _queuedUploadExecutor = queuedUploadExecutor;
            _restoreCoordinator = restoreCoordinator;
        }

        public async Task<CottonQueuedUploadExecutionResult> RunAsync(
            Uri instanceUri,
            Guid transferId,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            CottonQueuedUploadExecutionResult result = await _queuedUploadExecutor
                .ExecuteAsync(instanceUri, transferId, cancellationToken)
                .ConfigureAwait(false);
            if (result.Status != CottonQueuedUploadExecutionStatus.TransferNotQueued
                || result.Transfer?.Status != CottonTransferStatus.Running)
            {
                return result;
            }

            await _restoreCoordinator
                .RestoreAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            return await _queuedUploadExecutor
                .ExecuteAsync(instanceUri, transferId, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
