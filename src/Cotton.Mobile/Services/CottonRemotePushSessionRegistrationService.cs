using Cotton.Auth;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushSessionRegistrationService :
        ICottonRemotePushSessionRegistrationService,
        ICottonRemotePushTokenRefreshHandler,
        ICottonRemotePushSessionRegistrationStatusProvider
    {
        private readonly CottonRemotePushRegistrationCoordinator _registrationCoordinator;
        private readonly ICottonRemotePushDeviceTokenService _deviceTokenService;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonTokenStore _tokenStore;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ILogger<CottonRemotePushSessionRegistrationService> _logger;
        private readonly object _registrationStateLock = new();
        private CottonRemotePushRegistrationStatus? _lastRegistrationStatus;
        private DateTimeOffset? _lastRegistrationAttemptedAtUtc;

        public CottonRemotePushSessionRegistrationService(
            CottonRemotePushRegistrationCoordinator registrationCoordinator,
            ICottonRemotePushDeviceTokenService deviceTokenService,
            ICottonInstanceStore instanceStore,
            ICottonTokenStore tokenStore,
            ICottonMobileApplicationMetadata metadata,
            ILogger<CottonRemotePushSessionRegistrationService> logger)
        {
            ArgumentNullException.ThrowIfNull(registrationCoordinator);
            ArgumentNullException.ThrowIfNull(deviceTokenService);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(logger);

            _registrationCoordinator = registrationCoordinator;
            _deviceTokenService = deviceTokenService;
            _instanceStore = instanceStore;
            _tokenStore = tokenStore;
            _metadata = metadata;
            _logger = logger;
        }

        public CottonRemotePushRegistrationStatus? LastRegistrationStatus
        {
            get
            {
                lock (_registrationStateLock)
                {
                    return _lastRegistrationStatus;
                }
            }
        }

        public DateTimeOffset? LastRegistrationAttemptedAtUtc
        {
            get
            {
                lock (_registrationStateLock)
                {
                    return _lastRegistrationAttemptedAtUtc;
                }
            }
        }

        public async Task RegisterCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            try
            {
                CottonRemotePushRegistrationResult result =
                    await _registrationCoordinator
                        .RegisterCurrentAsync(
                            instanceUri,
                            _metadata.DeviceName,
                            _metadata.ApplicationVersion,
                            cancellationToken)
                        .ConfigureAwait(false);
                RecordRegistrationStatus(result.Status);
                LogRegistrationResult(result);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                RecordRegistrationStatus(CottonRemotePushRegistrationStatus.Unavailable);
                _logger.LogWarning(exception, "Failed to register the Cotton mobile remote push device token.");
            }
        }

        public async Task RevokeCurrentSessionBestEffortAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            try
            {
                CottonRemotePushDeviceTokenRevocationResult result =
                    await _deviceTokenService
                        .RevokeCurrentSessionAsync(instanceUri, cancellationToken)
                        .ConfigureAwait(false);
                _logger.LogInformation(
                    "Revoked {RevokedTokenCount} Cotton mobile remote push token(s) for the current session.",
                    result.RevokedTokens);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to revoke Cotton mobile remote push tokens for the current session.");
            }
        }

        public async Task HandleNewTokenAsync(
            string token,
            CancellationToken cancellationToken = default)
        {
            string normalizedToken = token?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                return;
            }

            try
            {
                Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
                if (instanceUri is null)
                {
                    return;
                }

                TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
                if (tokens is null
                    || string.IsNullOrWhiteSpace(tokens.AccessToken)
                    || string.IsNullOrWhiteSpace(tokens.RefreshToken))
                {
                    return;
                }

                var request = CottonRemotePushDeviceTokenRegistrationRequest.ForAndroidFirebase(
                    normalizedToken,
                    _metadata.DeviceName,
                    _metadata.ApplicationVersion);
                await _deviceTokenService
                    .RegisterCurrentAsync(instanceUri, request, cancellationToken)
                    .ConfigureAwait(false);
                RecordRegistrationStatus(CottonRemotePushRegistrationStatus.Registered);
                _logger.LogInformation("Refreshed the Cotton mobile remote push token registration.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                RecordRegistrationStatus(CottonRemotePushRegistrationStatus.Unavailable);
                _logger.LogWarning(exception, "Failed to refresh the Cotton mobile remote push token registration.");
            }
        }

        private void RecordRegistrationStatus(CottonRemotePushRegistrationStatus status)
        {
            lock (_registrationStateLock)
            {
                _lastRegistrationStatus = status;
                _lastRegistrationAttemptedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        private void LogRegistrationResult(CottonRemotePushRegistrationResult result)
        {
            switch (result.Status)
            {
                case CottonRemotePushRegistrationStatus.Registered:
                    _logger.LogInformation("Registered the Cotton mobile remote push token for the current session.");
                    break;
                case CottonRemotePushRegistrationStatus.NotConfigured:
                    _logger.LogInformation(
                        "Skipped Cotton mobile remote push token registration because the platform provider is not configured.");
                    break;
                default:
                    _logger.LogInformation(
                        "Skipped Cotton mobile remote push token registration because the platform token is unavailable.");
                    break;
            }
        }
    }
}
