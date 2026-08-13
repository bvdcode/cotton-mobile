// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncSessionService(
        IApplicationForegroundService foregroundService,
        ICottonAutomaticSyncBackgroundScheduler backgroundScheduler,
        CottonAutomaticSyncRunner runner,
        ILogger<CottonAutomaticSyncSessionService> logger) :
        ICottonAutomaticSyncSessionService,
        IDisposable
    {
        private readonly Lock _initializationGate = new();
        private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
        private readonly IApplicationForegroundService _foregroundService =
            foregroundService ?? throw new ArgumentNullException(nameof(foregroundService));
        private readonly ICottonAutomaticSyncBackgroundScheduler _backgroundScheduler =
            backgroundScheduler ?? throw new ArgumentNullException(nameof(backgroundScheduler));
        private readonly CottonAutomaticSyncRunner _runner =
            runner ?? throw new ArgumentNullException(nameof(runner));
        private readonly ILogger<CottonAutomaticSyncSessionService> _logger =
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
                    await CancelBackgroundWorkBestEffortAsync(cancellationToken).ConfigureAwait(false);
                    return;
                }

                await ScheduleBackgroundWorkBestEffortAsync(cancellationToken).ConfigureAwait(false);
                if (_foregroundService.IsForeground)
                {
                    await RunBestEffortAsync(
                            instanceUri,
                            CottonAutomaticSyncTrigger.ApplicationResumed,
                            cancellationToken)
                        .ConfigureAwait(false);
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
            _lifecycleGate.Dispose();
            GC.SuppressFinalize(this);
        }

        private void OnApplicationResumed(object? sender, EventArgs eventArgs)
        {
            _ = ResumeSafelyAsync();
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
                        await RunBestEffortAsync(
                                _instanceUri,
                                CottonAutomaticSyncTrigger.ApplicationResumed,
                                CancellationToken.None)
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
                CottonAutomaticSyncLog.ResumeFailed(_logger, exception);
            }
        }

        private async Task RunBestEffortAsync(
            Uri instanceUri,
            CottonAutomaticSyncTrigger trigger,
            CancellationToken cancellationToken)
        {
            try
            {
                await _runner.RunAsync(instanceUri, trigger, cancellationToken).ConfigureAwait(false);
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
    }
}
