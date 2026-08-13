// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRunner(
        ICottonSyncRootStore rootStore,
        ICottonDeviceToCloudSyncCoordinator coordinator,
        ILogger<CottonAutomaticSyncRunner> logger)
    {
        private readonly ICottonSyncRootStore _rootStore =
            rootStore ?? throw new ArgumentNullException(nameof(rootStore));
        private readonly ICottonDeviceToCloudSyncCoordinator _coordinator =
            coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        private readonly ILogger<CottonAutomaticSyncRunner> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        public async Task RunAsync(
            Uri instanceUri,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!Enum.IsDefined(trigger))
            {
                throw new ArgumentOutOfRangeException(nameof(trigger), "Automatic sync trigger is not supported.");
            }

            IReadOnlyList<CottonSyncRootSnapshot> roots = await _rootStore
                .LoadAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
            List<Exception> failures = [];
            foreach (CottonSyncRootSnapshot root in roots.Where(root => ShouldRun(root, trigger)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _coordinator.RunRootAsync(instanceUri, root, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    CottonAutomaticSyncLog.RootFailed(_logger, root.Id, exception);
                    failures.Add(exception);
                }
            }

            if (failures.Count > 0)
            {
                throw new AggregateException("One or more automatic sync roots failed.", failures);
            }
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
