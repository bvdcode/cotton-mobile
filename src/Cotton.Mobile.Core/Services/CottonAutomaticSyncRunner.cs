// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRunner(
        ICottonSyncRootStore rootStore,
        ICottonDeviceToCloudSyncCoordinator coordinator,
        ICottonAutomaticSyncStatusStore statusStore,
        TimeProvider timeProvider,
        ILogger<CottonAutomaticSyncRunner> logger) : ICottonAutomaticSyncRunner
    {
        private readonly ICottonSyncRootStore _rootStore =
            rootStore ?? throw new ArgumentNullException(nameof(rootStore));
        private readonly ICottonDeviceToCloudSyncCoordinator _coordinator =
            coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        private readonly ICottonAutomaticSyncStatusStore _statusStore =
            statusStore ?? throw new ArgumentNullException(nameof(statusStore));
        private readonly TimeProvider _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        private readonly ILogger<CottonAutomaticSyncRunner> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        public Task<CottonAutomaticSyncRunResult> RunAsync(
            Uri instanceUri,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            return RunSelectedAsync(
                instanceUri,
                root => ShouldRun(root, trigger),
                cancellationToken);
        }

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            Uri instanceUri,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(rootIds);
            HashSet<Guid> selectedRootIds = [.. rootIds];
            if (selectedRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids cannot be empty.", nameof(rootIds));
            }

            return RunSelectedAsync(
                instanceUri,
                root => selectedRootIds.Contains(root.Id),
                cancellationToken);
        }

        private async Task<CottonAutomaticSyncRunResult> RunSelectedAsync(
            Uri instanceUri,
            Func<CottonSyncRootSnapshot, bool> shouldRun,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore
                .LoadAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlySet<Guid> activeRootIds = roots.Select(root => root.Id).ToHashSet();
            List<Guid> succeededRootIds = [];
            List<Guid> failedRootIds = [];
            List<CottonAutomaticSyncRootStatusSnapshot> updatedStatuses = [];
            foreach (CottonSyncRootSnapshot root in roots.Where(shouldRun))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _coordinator.RunRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
                    succeededRootIds.Add(root.Id);
                    updatedStatuses.Add(CreateStatus(root.Id, CottonAutomaticSyncOutcome.Succeeded));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    CottonAutomaticSyncLog.RootFailed(_logger, root.Id, exception);
                    failedRootIds.Add(root.Id);
                    updatedStatuses.Add(CreateStatus(root.Id, CottonAutomaticSyncOutcome.Failed));
                }
            }

            await _statusStore
                .UpdateAsync(instanceUri, activeRootIds, updatedStatuses, cancellationToken)
                .ConfigureAwait(false);
            return new CottonAutomaticSyncRunResult(succeededRootIds, failedRootIds);
        }

        private CottonAutomaticSyncRootStatusSnapshot CreateStatus(
            Guid rootId,
            CottonAutomaticSyncOutcome outcome)
        {
            return new CottonAutomaticSyncRootStatusSnapshot(
                rootId,
                outcome,
                _timeProvider.GetUtcNow().UtcDateTime);
        }

        private static bool ShouldRun(
            CottonSyncRootSnapshot root,
            CottonAutomaticSyncTrigger trigger)
        {
            return trigger switch
            {
                CottonAutomaticSyncTrigger.ApplicationResumed => true,
                CottonAutomaticSyncTrigger.PeriodicReconciliation => true,
                CottonAutomaticSyncTrigger.MediaStoreChanged => root.LocalRoot.UsesMediaStore,
                _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, "Automatic sync trigger is not supported."),
            };
        }
    }
}
