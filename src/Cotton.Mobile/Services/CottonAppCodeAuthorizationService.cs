// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CottonAppCodeAuthorizationService : ICottonAppCodeAuthorizationService
    {
        private static readonly TimeSpan RestoreTimeout = TimeSpan.FromSeconds(8);

        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ICottonPendingAppCodeSessionStore _pendingSessionStore;
        private readonly IBrowser _browser;
        private readonly IApplicationForegroundService _foregroundService;
        private readonly ILogger<CottonAppCodeAuthorizationService> _logger;

        public CottonAppCodeAuthorizationService(
            ICottonClientFactory clientFactory,
            ICottonMobileApplicationMetadata metadata,
            ICottonPendingAppCodeSessionStore pendingSessionStore,
            IBrowser browser,
            IApplicationForegroundService foregroundService,
            ILogger<CottonAppCodeAuthorizationService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(pendingSessionStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(logger);

            _clientFactory = clientFactory;
            _metadata = metadata;
            _pendingSessionStore = pendingSessionStore;
            _browser = browser;
            _foregroundService = foregroundService;
            _logger = logger;
        }

        public async Task<CottonSessionResult> SignInAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            AppCodeAuthorizationSession session = await client.Auth.StartAppCodeAsync(
                new AppCodeStartRequestDto
                {
                    ApplicationName = _metadata.ApplicationName,
                    ApplicationVersion = _metadata.ApplicationVersion,
                    DeviceName = _metadata.DeviceName,
                },
                cancellationToken).ConfigureAwait(false);
            await _pendingSessionStore
                .SaveAsync(CreatePendingSession(instanceUri, session), cancellationToken)
                .ConfigureAwait(false);

            long resumeVersionCheckpoint = _foregroundService.CurrentResumeVersion;
            bool browserOpened;
            try
            {
                browserOpened = await MainThread.InvokeOnMainThreadAsync(
                    () => _browser.OpenAsync(session.ApprovalUri, CottonBrowserLaunchOptions.SystemPreferred()))
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Failed to open Cotton mobile app-code authorization browser.");
                await ClearPendingBestEffortAsync("browser open failure").ConfigureAwait(false);
                throw;
            }

            if (!browserOpened)
            {
                await ClearPendingBestEffortAsync("browser unavailable").ConfigureAwait(false);
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.BrowserUnavailable, instanceUri);
            }

            try
            {
                bool returnedBeforeExpiration = await WaitForBrowserReturnAsync(
                    session,
                    resumeVersionCheckpoint,
                    cancellationToken).ConfigureAwait(false);
                if (!returnedBeforeExpiration)
                {
                    await ClearPendingBestEffortAsync("browser return timeout").ConfigureAwait(false);
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
                }

                CottonSessionResult result = await CottonAppCodeAuthorizationPoller.PollUntilCompleteAsync(
                    client,
                    session,
                    instanceUri,
                    cancellationToken).ConfigureAwait(false);
                await ClearPendingBestEffortAsync("browser authorization completion").ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await ClearPendingBestEffortAsync("browser authorization cancellation").ConfigureAwait(false);
                throw;
            }
        }

        public async Task<CottonSessionResult> RestorePendingAsync(
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            CottonPendingAppCodeSession? pendingSession = await _pendingSessionStore
                .GetAsync(cancellationToken)
                .ConfigureAwait(false);
            if (pendingSession is null)
            {
                return CottonSessionResult.Unauthenticated(instanceUri);
            }

            if (!Uri.Equals(pendingSession.InstanceUri, instanceUri))
            {
                await ClearPendingBestEffortAsync("pending authorization instance mismatch").ConfigureAwait(false);
                return CottonSessionResult.Unauthenticated(instanceUri);
            }

            if (pendingSession.ExpiresAt <= DateTime.UtcNow)
            {
                await ClearPendingBestEffortAsync("pending authorization expiration").ConfigureAwait(false);
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
            }

            using var restoreTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            restoreTimeout.CancelAfter(RestoreTimeout);
            try
            {
                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                CottonSessionResult result = await CottonAppCodeAuthorizationPoller.PollUntilCompleteAsync(
                    client,
                    CreateAuthorizationSession(pendingSession),
                    instanceUri,
                    restoreTimeout.Token).ConfigureAwait(false);
                await ClearPendingBestEffortAsync("pending authorization restore completion").ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested && restoreTimeout.IsCancellationRequested)
            {
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationPending, instanceUri);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to restore Cotton mobile pending app-code authorization; keeping it for retry.");
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationPending, instanceUri);
            }
        }

        public async Task ClearPendingBestEffortAsync(string reason)
        {
            try
            {
                await _pendingSessionStore.ClearAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to clear Cotton mobile pending authorization after {Reason}.",
                    reason);
            }
        }

        private async Task<bool> WaitForBrowserReturnAsync(
            AppCodeAuthorizationSession session,
            long resumeVersionCheckpoint,
            CancellationToken cancellationToken)
        {
            TimeSpan remaining = session.ExpiresAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(remaining);
            try
            {
                await _foregroundService
                    .WaitForNextResumeAsync(resumeVersionCheckpoint, timeout.Token)
                    .ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        private static CottonPendingAppCodeSession CreatePendingSession(
            Uri instanceUri,
            AppCodeAuthorizationSession session)
        {
            return new CottonPendingAppCodeSession
            {
                InstanceUri = instanceUri,
                ApprovalId = session.ApprovalId,
                ApprovalUri = session.ApprovalUri,
                PollToken = session.PollToken,
                ExpiresAt = session.ExpiresAt,
                PollInterval = session.PollInterval,
            };
        }

        private static AppCodeAuthorizationSession CreateAuthorizationSession(
            CottonPendingAppCodeSession pendingSession)
        {
            return new AppCodeAuthorizationSession
            {
                ApprovalId = pendingSession.ApprovalId,
                ApprovalUri = pendingSession.ApprovalUri,
                PollToken = pendingSession.PollToken,
                ExpiresAt = pendingSession.ExpiresAt,
                PollInterval = pendingSession.PollInterval,
            };
        }
    }
}
