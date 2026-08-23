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
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sessionScope);
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            CottonSyncDiagnosticLog.AutomaticRequested(_logger, trigger);

            return RunSelectedAsync(
                sessionScope,
                root => ShouldRun(root, trigger),
                cancellationToken);
        }

        public Task<CottonAutomaticSyncRunResult> RunRootsAsync(
            CottonAuthenticatedSessionScope sessionScope,
            IReadOnlyCollection<Guid> rootIds,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(sessionScope);
            ArgumentNullException.ThrowIfNull(rootIds);
            HashSet<Guid> selectedRootIds = [.. rootIds];
            if (selectedRootIds.Contains(Guid.Empty))
            {
                throw new ArgumentException("Automatic sync root ids cannot be empty.", nameof(rootIds));
            }

            CottonSyncDiagnosticLog.AutomaticRootsRequested(_logger, selectedRootIds.Count);

            return RunSelectedAsync(
                sessionScope,
                root => selectedRootIds.Contains(root.Id),
                cancellationToken);
        }

        private async Task<CottonAutomaticSyncRunResult> RunSelectedAsync(
            CottonAuthenticatedSessionScope sessionScope,
            Func<CottonSyncRootSnapshot, bool> shouldRun,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore
                .LoadAsync(sessionScope.InstanceUri, cancellationToken)
                .ConfigureAwait(false);
            CottonSyncRootSnapshot[] accountRoots = [.. roots.Where(root => string.Equals(
                root.AccountScopeKey,
                sessionScope.AccountScopeKey,
                StringComparison.Ordinal))];
            IReadOnlySet<Guid> activeRootIds = roots.Select(root => root.Id).ToHashSet();
            CottonSyncRootSnapshot[] selectedRoots = [.. accountRoots.Where(shouldRun)];
            CottonSyncDiagnosticLog.AutomaticRootsSelected(_logger, selectedRoots.Length, accountRoots.Length);
            List<Guid> succeededRootIds = [];
            List<CottonAutomaticSyncFailure> failures = [];
            List<CottonAutomaticSyncRootStatusSnapshot> updatedStatuses = [];
            foreach (CottonSyncRootSnapshot root in selectedRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    CottonDeviceToCloudSyncRunSummary summary = await _coordinator
                        .RunRootAsync(sessionScope.InstanceUri, root, cancellationToken)
                        .ConfigureAwait(false);
                    DateTime completedAt = _timeProvider.GetUtcNow().UtcDateTime;
                    if (summary.HasBlockedItems)
                    {
                        CottonAutomaticSyncFailure failure = new(
                            root.Id,
                            CottonAutomaticSyncFailureKind.ActionRequired);
                        failures.Add(failure);
                        updatedStatuses.Add(CottonAutomaticSyncRootStatusSnapshot.Failed(
                            root.Id,
                            completedAt,
                            failure.Kind));
                    }
                    else
                    {
                        succeededRootIds.Add(root.Id);
                        updatedStatuses.Add(CottonAutomaticSyncRootStatusSnapshot.Succeeded(
                            root.Id,
                            completedAt));
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    CottonAutomaticSyncLog.RootFailed(_logger, root.Id, exception);
                    CottonAutomaticSyncFailure failure = new(
                        root.Id,
                        CottonAutomaticSyncFailureClassifier.Classify(exception));
                    failures.Add(failure);
                    updatedStatuses.Add(CottonAutomaticSyncRootStatusSnapshot.Failed(
                        root.Id,
                        _timeProvider.GetUtcNow().UtcDateTime,
                        failure.Kind));
                }
            }

            await _statusStore
                .UpdateAsync(sessionScope.InstanceUri, activeRootIds, updatedStatuses, cancellationToken)
                .ConfigureAwait(false);
            CottonSyncDiagnosticLog.AutomaticCompleted(
                _logger,
                succeededRootIds.Count,
                failures.Count);
            return new CottonAutomaticSyncRunResult(succeededRootIds, failures);
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
