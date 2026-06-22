// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net;
using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CottonSessionService : ICottonSessionService
    {
        private static readonly TimeSpan MinimumPollDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan PendingAuthorizationRestoreTimeout = TimeSpan.FromSeconds(8);

        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ICottonTokenStore _tokenStore;
        private readonly ICottonPendingAppCodeSessionStore _pendingSessionStore;
        private readonly IBrowser _browser;
        private readonly IApplicationForegroundService _foregroundService;
        private readonly ILogger<CottonSessionService> _logger;

        public CottonSessionService(
            ICottonClientFactory clientFactory,
            ICottonInstanceStore instanceStore,
            ICottonMobileApplicationMetadata metadata,
            ICottonTokenStore tokenStore,
            ICottonPendingAppCodeSessionStore pendingSessionStore,
            IBrowser browser,
            IApplicationForegroundService foregroundService,
            ILogger<CottonSessionService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(pendingSessionStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(foregroundService);
            ArgumentNullException.ThrowIfNull(logger);

            _clientFactory = clientFactory;
            _instanceStore = instanceStore;
            _metadata = metadata;
            _tokenStore = tokenStore;
            _pendingSessionStore = pendingSessionStore;
            _browser = browser;
            _foregroundService = foregroundService;
            _logger = logger;
        }

        public async Task<CottonSessionResult> RestoreAsync(CancellationToken cancellationToken = default)
        {
            Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (instanceUri is null)
            {
                await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
                return CottonSessionResult.Unauthenticated();
            }

            TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (tokens is null)
            {
                return await RestorePendingAuthorizationAsync(instanceUri, cancellationToken).ConfigureAwait(false);
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            try
            {
                TokenPairDto refreshedTokens = await client.Auth
                    .RefreshAsync(tokens.RefreshToken, cancellationToken)
                    .ConfigureAwait(false);
                await _tokenStore.SaveAsync(refreshedTokens, cancellationToken).ConfigureAwait(false);
                UserDto user = await client.Auth.MeAsync(cancellationToken).ConfigureAwait(false);
                await ClearPendingSessionBestEffortAsync("session restore").ConfigureAwait(false);

                return CottonSessionResult.Authenticated(instanceUri, user);
            }
            catch (CottonApiException exception) when (IsAuthorizationFailure(exception))
            {
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.SessionExpired, instanceUri);
            }
        }

        public async Task<CottonSessionResult> SignInWithBrowserAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
            await _instanceStore.SaveAsync(instanceUri, cancellationToken).ConfigureAwait(false);

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
                await ClearPendingSessionBestEffortAsync("browser open failure").ConfigureAwait(false);
                throw;
            }

            if (!browserOpened)
            {
                await ClearPendingSessionBestEffortAsync("browser unavailable").ConfigureAwait(false);
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
                    await ClearPendingSessionBestEffortAsync("browser return timeout").ConfigureAwait(false);
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
                }

                CottonSessionResult result = await PollUntilCompleteAsync(
                    client,
                    session,
                    instanceUri,
                    cancellationToken).ConfigureAwait(false);
                await ClearPendingSessionBestEffortAsync("browser authorization completion").ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await ClearPendingSessionBestEffortAsync("browser authorization cancellation").ConfigureAwait(false);
                throw;
            }
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (instanceUri is null)
            {
                await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                await client.Auth.LogoutAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Cotton mobile remote logout failed; clearing local session.");
            }
            finally
            {
                await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ClearLocalSessionAsync(CancellationToken cancellationToken = default)
        {
            List<Exception> failures = [];
            await TryClearLocalSessionAreaAsync(
                _tokenStore.ClearAsync,
                "tokens",
                failures,
                cancellationToken).ConfigureAwait(false);
            await TryClearLocalSessionAreaAsync(
                _pendingSessionStore.ClearAsync,
                "pending authorization",
                failures,
                cancellationToken).ConfigureAwait(false);
            await TryClearLocalSessionAreaAsync(
                _instanceStore.ClearAsync,
                "instance",
                failures,
                cancellationToken).ConfigureAwait(false);

            if (failures.Count == 1)
            {
                throw new InvalidOperationException("Failed to clear one Cotton mobile session area.", failures[0]);
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Failed to clear Cotton mobile local session.", failures);
            }
        }

        private async Task TryClearLocalSessionAreaAsync(
            Func<CancellationToken, Task> clearAsync,
            string sessionAreaName,
            List<Exception> failures,
            CancellationToken cancellationToken)
        {
            try
            {
                await clearAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Failed to clear Cotton mobile {SessionAreaName}.", sessionAreaName);
                failures.Add(exception);
            }
        }

        private async Task<CottonSessionResult> RestorePendingAuthorizationAsync(
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
                await ClearPendingSessionBestEffortAsync("pending authorization instance mismatch").ConfigureAwait(false);
                return CottonSessionResult.Unauthenticated(instanceUri);
            }

            if (pendingSession.ExpiresAt <= DateTime.UtcNow)
            {
                await ClearPendingSessionBestEffortAsync("pending authorization expiration").ConfigureAwait(false);
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
            }

            using var restoreTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            restoreTimeout.CancelAfter(PendingAuthorizationRestoreTimeout);

            try
            {
                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                CottonSessionResult result = await PollUntilCompleteAsync(
                    client,
                    CreateAuthorizationSession(pendingSession),
                    instanceUri,
                    restoreTimeout.Token).ConfigureAwait(false);
                await ClearPendingSessionBestEffortAsync("pending authorization restore completion").ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && restoreTimeout.IsCancellationRequested)
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

        private async Task ClearPendingSessionBestEffortAsync(string reason)
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

        private async Task<CottonSessionResult> PollUntilCompleteAsync(
            ICottonCloudClient client,
            AppCodeAuthorizationSession session,
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            while (DateTime.UtcNow < session.ExpiresAt)
            {
                AppCodePollResult poll = await client.Auth.PollAppCodeAsync(
                    session.PollToken,
                    cancellationToken).ConfigureAwait(false);

                CottonSessionResult? result = await TryCreateCompletedResultAsync(
                    client,
                    poll,
                    instanceUri,
                    cancellationToken).ConfigureAwait(false);
                if (result is not null)
                {
                    return result;
                }

                await DelayBeforeNextPollAsync(
                    ResolvePollDelay(session, poll),
                    session.ExpiresAt,
                    cancellationToken).ConfigureAwait(false);
            }

            return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
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

        private async Task<CottonSessionResult?> TryCreateCompletedResultAsync(
            ICottonCloudClient client,
            AppCodePollResult poll,
            Uri instanceUri,
            CancellationToken cancellationToken)
        {
            switch (poll.Status)
            {
                case AppCodePollStatus.Approved:
                    UserDto user = await client.Auth.MeAsync(cancellationToken).ConfigureAwait(false);
                    return CottonSessionResult.Authenticated(instanceUri, user);
                case AppCodePollStatus.Pending:
                case AppCodePollStatus.TooManyRequests:
                    return null;
                case AppCodePollStatus.Denied:
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationDenied, instanceUri, poll.Error);
                case AppCodePollStatus.Expired:
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationExpired, instanceUri, poll.Error);
                case AppCodePollStatus.NotFound:
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationNotFound, instanceUri, poll.Error);
                default:
                    return CottonSessionResult.FromStatus(CottonSessionResultStatus.AuthorizationFailed, instanceUri, poll.Error);
            }
        }

        private static TimeSpan ResolvePollDelay(AppCodeAuthorizationSession session, AppCodePollResult poll)
        {
            TimeSpan delay = poll.RetryAfter ?? session.PollInterval;
            return delay < MinimumPollDelay ? MinimumPollDelay : delay;
        }

        private static async Task DelayBeforeNextPollAsync(
            TimeSpan delay,
            DateTime expiresAt,
            CancellationToken cancellationToken)
        {
            TimeSpan remaining = expiresAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan effectiveDelay = delay < remaining ? delay : remaining;
            await Task.Delay(effectiveDelay, cancellationToken).ConfigureAwait(false);
        }

        private static bool IsAuthorizationFailure(CottonApiException exception)
        {
            return exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        }
    }
}
