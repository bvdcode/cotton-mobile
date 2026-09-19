// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncSessionService(
        IApplicationForegroundService foregroundService,
        ICottonAutomaticSyncBackgroundScheduler backgroundScheduler,
        CottonAutomaticSyncDispatcher dispatcher,
        ILogger<CottonAutomaticSyncSessionService> logger) :
        ICottonAutomaticSyncSessionService,
        IDisposable
    {
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly IApplicationForegroundService _foregroundService =
            foregroundService ?? throw new ArgumentNullException(nameof(foregroundService));
        private readonly ICottonAutomaticSyncBackgroundScheduler _backgroundScheduler =
            backgroundScheduler ?? throw new ArgumentNullException(nameof(backgroundScheduler));
        private readonly CottonAutomaticSyncDispatcher _dispatcher =
            dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        private readonly ILogger<CottonAutomaticSyncSessionService> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        private CottonAuthenticatedSessionScope? _sessionScope;

        public async Task SetSessionAsync(
            CottonAuthenticatedSessionScope? sessionScope,
            CancellationToken cancellationToken = default)
        {
            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CottonAuthenticatedSessionScope? previousSessionScope = _sessionScope;
                _sessionScope = sessionScope;
                if (previousSessionScope is not null && !HasSameIdentity(previousSessionScope, sessionScope))
                {
                    _dispatcher.Cancel(previousSessionScope);
                }

                if (sessionScope is null)
                {
                    await CancelBackgroundWorkBestEffortAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                await ScheduleBackgroundWorkBestEffortAsync(cancellationToken).ConfigureAwait(false);
                if (_foregroundService.IsForeground)
                {
                    _ = RunBestEffortAsync(
                        sessionScope,
                        CottonAutomaticSyncTrigger.ForegroundSessionStarted,
                        CancellationToken.None);
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public void Dispose()
        {
            if (_sessionScope is not null)
            {
                _dispatcher.Cancel(_sessionScope);
            }

            _lifecycleGate.Dispose();
            GC.SuppressFinalize(this);
        }

        private async Task RunBestEffortAsync(
            CottonAuthenticatedSessionScope sessionScope,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken)
        {
            try
            {
                CottonAutomaticSyncRunResult result = await _dispatcher
                    .RunAsync(sessionScope, trigger, cancellationToken)
                    .ConfigureAwait(false);
                if (result.RetryableRootIds.Count > 0)
                {
                    await _backgroundScheduler
                        .ScheduleRootRetriesAsync(result.RetryableRootIds, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException exception)
            {
                CottonAutomaticSyncLog.RunCanceled(_logger, exception);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonAutomaticSyncLog.RunFailed(_logger, exception);
            }
        }

        private async Task ScheduleBackgroundWorkBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _backgroundScheduler.ScheduleAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonAutomaticSyncLog.BackgroundScheduleFailed(_logger, exception);
            }
        }

        private async Task CancelBackgroundWorkBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _backgroundScheduler.CancelAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonAutomaticSyncLog.BackgroundCancelFailed(_logger, exception);
            }
        }

        private static bool HasSameIdentity(
            CottonAuthenticatedSessionScope left,
            CottonAuthenticatedSessionScope? right)
        {
            return right is not null
                && Uri.Equals(left.InstanceUri, right.InstanceUri)
                && string.Equals(left.AccountScopeKey, right.AccountScopeKey, StringComparison.Ordinal);
        }
    }
}
