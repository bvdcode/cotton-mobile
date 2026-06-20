using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonAppLockCoordinator : ICottonAppLockCoordinator
    {
        private readonly Lock _gate = new();
        private readonly IApplicationForegroundService _foregroundService;
        private readonly ICottonAppLockSettingsStore _settingsStore;
        private readonly ICottonAppLockCapabilityService _capabilityService;
        private readonly ICottonAppLockRuntimeStateStore _runtimeStateStore;
        private readonly CottonAppLockPolicy _appLockPolicy;
        private readonly IAppLockGateService _appLockGateService;
        private readonly ILogger<CottonAppLockCoordinator> _logger;
        private bool _isLockGateOpen;
        private bool _started;

        public CottonAppLockCoordinator(
            IApplicationForegroundService foregroundService,
            ICottonAppLockSettingsStore settingsStore,
            ICottonAppLockCapabilityService capabilityService,
            ICottonAppLockRuntimeStateStore runtimeStateStore,
            CottonAppLockPolicy appLockPolicy,
            IAppLockGateService appLockGateService,
            ILogger<CottonAppLockCoordinator> logger)
        {
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(settingsStore);
            ArgumentNullException.ThrowIfNull(capabilityService);
            ArgumentNullException.ThrowIfNull(runtimeStateStore);
            ArgumentNullException.ThrowIfNull(appLockPolicy);
            ArgumentNullException.ThrowIfNull(appLockGateService);
            ArgumentNullException.ThrowIfNull(logger);

            _foregroundService = foregroundService;
            _settingsStore = settingsStore;
            _capabilityService = capabilityService;
            _runtimeStateStore = runtimeStateStore;
            _appLockPolicy = appLockPolicy;
            _appLockGateService = appLockGateService;
            _logger = logger;
        }

        public void Start()
        {
            lock (_gate)
            {
                if (_started)
                {
                    return;
                }

                _started = true;
            }

            _foregroundService.Stopped += ForegroundService_Stopped;
            _foregroundService.Resumed += ForegroundService_Resumed;
        }

        private void ForegroundService_Stopped(object? sender, EventArgs e)
        {
            _ = RecordBackgroundedAsync();
        }

        private void ForegroundService_Resumed(object? sender, EventArgs e)
        {
            _ = CheckAppLockOnResumeAsync();
        }

        private async Task RecordBackgroundedAsync()
        {
            try
            {
                DateTimeOffset backgroundedAtUtc = _foregroundService.LastStoppedAtUtc ?? DateTimeOffset.UtcNow;
                CottonAppLockRuntimeState runtimeState = await _runtimeStateStore.GetAsync().ConfigureAwait(false);
                await _runtimeStateStore
                    .SaveAsync(runtimeState.WithBackgroundedAt(backgroundedAtUtc))
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to record Cotton mobile app lock background time.");
            }
        }

        private async Task CheckAppLockOnResumeAsync()
        {
            try
            {
                if (IsLockGateOpen())
                {
                    return;
                }

                CottonAppLockSettings settings = await _settingsStore.GetAsync().ConfigureAwait(false);
                CottonAppLockCapabilitySnapshot capability =
                    await _capabilityService.GetCapabilityAsync().ConfigureAwait(false);
                CottonAppLockRuntimeState runtimeState =
                    await _runtimeStateStore.GetAsync().ConfigureAwait(false);

                if (!_appLockPolicy.ShouldLock(
                        settings,
                        capability,
                        runtimeState,
                        DateTimeOffset.UtcNow))
                {
                    return;
                }

                if (!TryOpenLockGate())
                {
                    return;
                }

                await UnlockAsync(runtimeState).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to run Cotton mobile app lock resume check.");
                CloseLockGate();
            }
        }

        private async Task UnlockAsync(CottonAppLockRuntimeState runtimeState)
        {
            try
            {
                CottonDeviceUnlockResult result =
                    await _appLockGateService.ShowAndUnlockAsync().ConfigureAwait(false);
                if (!result.IsSucceeded)
                {
                    return;
                }

                await _runtimeStateStore
                    .SaveAsync(runtimeState.WithUnlockedAt(DateTimeOffset.UtcNow))
                    .ConfigureAwait(false);
            }
            finally
            {
                CloseLockGate();
            }
        }

        private bool IsLockGateOpen()
        {
            lock (_gate)
            {
                return _isLockGateOpen;
            }
        }

        private bool TryOpenLockGate()
        {
            lock (_gate)
            {
                if (_isLockGateOpen)
                {
                    return false;
                }

                _isLockGateOpen = true;
                return true;
            }
        }

        private void CloseLockGate()
        {
            lock (_gate)
            {
                _isLockGateOpen = false;
            }
        }
    }
}
