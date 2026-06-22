// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonRemotePushDiagnosticsService : ICottonRemotePushDiagnosticsService
    {
        private readonly ICottonRemotePushPlatformTokenProvider _platformTokenProvider;
        private readonly ICottonRemotePushSessionRegistrationStatusProvider _registrationStatusProvider;
        private readonly ILogger<CottonRemotePushDiagnosticsService> _logger;

        public CottonRemotePushDiagnosticsService(
            ICottonRemotePushPlatformTokenProvider platformTokenProvider,
            ICottonRemotePushSessionRegistrationStatusProvider registrationStatusProvider,
            ILogger<CottonRemotePushDiagnosticsService> logger)
        {
            ArgumentNullException.ThrowIfNull(platformTokenProvider);
            ArgumentNullException.ThrowIfNull(registrationStatusProvider);
            ArgumentNullException.ThrowIfNull(logger);

            _platformTokenProvider = platformTokenProvider;
            _registrationStatusProvider = registrationStatusProvider;
            _logger = logger;
        }

        public async Task<CottonRemotePushDiagnosticsSnapshot> GetSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            CottonRemotePushPlatformTokenSnapshot token;
            try
            {
                token = await _platformTokenProvider.GetCurrentTokenAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to inspect Cotton mobile remote push platform token.");
                token = CottonRemotePushPlatformTokenSnapshot.Unavailable(
                    CottonRemotePushProviderKind.FirebaseCloudMessaging,
                    CottonRemotePushMobilePlatform.Android,
                    "Remote push token inspection failed.");
            }

            return new CottonRemotePushDiagnosticsSnapshot(
                CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend,
                token,
                _registrationStatusProvider.LastRegistrationStatus,
                _registrationStatusProvider.LastRegistrationAttemptedAtUtc);
        }
    }
}
