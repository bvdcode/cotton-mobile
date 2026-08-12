// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationSessionService(
        IApplicationForegroundService foregroundService,
        ICottonNotificationPermissionService permissionService,
        ICottonNotificationBackgroundScheduler backgroundScheduler,
        ICottonNotificationRealtimeService realtimeService,
        ICottonNotificationPollingService pollingService,
        ILogger<CottonNotificationSessionService> logger) :
        ICottonNotificationSessionService,
        IDisposable
    {
        private readonly Lock _initializationGate = new();
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly IApplicationForegroundService _foregroundService =
            foregroundService ?? throw new ArgumentNullException(nameof(foregroundService));
        private readonly ICottonNotificationPermissionService _permissionService =
            permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        private readonly ICottonNotificationBackgroundScheduler _backgroundScheduler =
            backgroundScheduler ?? throw new ArgumentNullException(nameof(backgroundScheduler));
        private readonly ICottonNotificationRealtimeService _realtimeService =
            realtimeService ?? throw new ArgumentNullException(nameof(realtimeService));
        private readonly ICottonNotificationPollingService _pollingService =
            pollingService ?? throw new ArgumentNullException(nameof(pollingService));
        private readonly ILogger<CottonNotificationSessionService> _logger =
            logger ?? throw new ArgumentNullException(nameof(logger));

        private Uri? _instanceUri;
        private bool _initialized;

        public void Initialize()
        {
            lock (_initializationGate)
            {
                if (_initialized)
                {
                    return;
                }

                _foregroundService.Resumed += OnApplicationResumed;
                _foregroundService.Stopped += OnApplicationStopped;
                _initialized = true;
            }
        }

        public async Task SetSessionAsync(
            Uri? instanceUri,
            CancellationToken cancellationToken = default)
        {
            await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _instanceUri = instanceUri;
                if (instanceUri is null)
                {
                    await CancelBackgroundPollingBestEffortAsync(cancellationToken).ConfigureAwait(false);
                    await StopRealtimeBestEffortAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                await ScheduleBackgroundPollingBestEffortAsync(cancellationToken).ConfigureAwait(false);
                if (_foregroundService.IsForeground)
                {
                    await StartForegroundDeliveryAsync(instanceUri, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                _lifecycleGate.Release();
            }
        }

        public void Dispose()
        {
            _foregroundService.Resumed -= OnApplicationResumed;
            _foregroundService.Stopped -= OnApplicationStopped;
            _lifecycleGate.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnApplicationResumed(object? sender, EventArgs eventArgs)
        {
            _ = ResumeSafelyAsync();
        }

        private void OnApplicationStopped(object? sender, EventArgs eventArgs)
        {
            _ = StopRealtimeSafelyAsync();
        }

        private async Task ResumeSafelyAsync()
        {
            try
            {
                await _lifecycleGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (_instanceUri is not null)
                    {
                        await StartForegroundDeliveryAsync(_instanceUri, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    _lifecycleGate.Release();
                }
            }
            catch (Exception exception)
            {
                CottonNotificationSessionLog.ResumeFailed(_logger, exception);
            }
        }

        private async Task StopRealtimeSafelyAsync()
        {
            try
            {
                await _lifecycleGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    await StopRealtimeBestEffortAsync(CancellationToken.None).ConfigureAwait(false);
                }
                finally
                {
                    _lifecycleGate.Release();
                }
            }
            catch (Exception exception)
            {
                CottonNotificationSessionLog.RealtimeStopFailed(_logger, exception);
            }
        }

        private async Task StartForegroundDeliveryAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            await RequestPermissionBestEffortAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await _realtimeService.StartAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.RealtimeStartFailed(_logger, exception);
            }

            try
            {
                await _pollingService.CheckAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.NotificationFetchFailed(_logger, exception);
            }
        }

        private async Task RequestPermissionBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _permissionService.RequestIfNeededAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.PermissionRequestFailed(_logger, exception);
            }
        }

        private async Task ScheduleBackgroundPollingBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _backgroundScheduler.ScheduleAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.BackgroundScheduleFailed(_logger, exception);
            }
        }

        private async Task CancelBackgroundPollingBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _backgroundScheduler.CancelAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.BackgroundCancelFailed(_logger, exception);
            }
        }

        private async Task StopRealtimeBestEffortAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _realtimeService.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                CottonNotificationSessionLog.RealtimeStopFailed(_logger, exception);
            }
        }
    }
}
