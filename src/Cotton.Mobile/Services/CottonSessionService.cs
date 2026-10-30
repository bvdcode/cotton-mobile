// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Net;
using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonSessionService : ICottonSessionService
    {
        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonTokenStore _tokenStore;
        private readonly ICottonPendingAppCodeSessionStore _pendingSessionStore;
        private readonly ICottonNotificationCursorStore _notificationCursorStore;
        private readonly ICottonAppCodeAuthorizationService _appCodeAuthorization;
        private readonly ILogger<CottonSessionService> _logger;

        public CottonSessionService(
            ICottonClientFactory clientFactory,
            ICottonInstanceStore instanceStore,
            ICottonTokenStore tokenStore,
            ICottonPendingAppCodeSessionStore pendingSessionStore,
            ICottonNotificationCursorStore notificationCursorStore,
            ICottonAppCodeAuthorizationService appCodeAuthorization,
            ILogger<CottonSessionService> logger)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(pendingSessionStore);
            ArgumentNullException.ThrowIfNull(notificationCursorStore);
            ArgumentNullException.ThrowIfNull(appCodeAuthorization);
            ArgumentNullException.ThrowIfNull(logger);

            _clientFactory = clientFactory;
            _instanceStore = instanceStore;
            _tokenStore = tokenStore;
            _pendingSessionStore = pendingSessionStore;
            _notificationCursorStore = notificationCursorStore;
            _appCodeAuthorization = appCodeAuthorization;
            _logger = logger;
        }

        public async Task<Uri?> GetRememberedSessionInstanceAsync(
            CancellationToken cancellationToken = default)
        {
            Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (instanceUri is null)
            {
                return null;
            }

            TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
            return tokens is null ? null : instanceUri;
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
                return await _appCodeAuthorization
                    .RestorePendingAsync(instanceUri, cancellationToken)
                    .ConfigureAwait(false);
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            try
            {
                TokenPairDto refreshedTokens = await client.Auth
                    .RefreshAsync(tokens.RefreshToken, cancellationToken)
                    .ConfigureAwait(false);
                await _tokenStore.SaveAsync(refreshedTokens, cancellationToken).ConfigureAwait(false);
                UserDto user = await client.Auth.MeAsync(cancellationToken).ConfigureAwait(false);
                await _appCodeAuthorization
                    .ClearPendingBestEffortAsync("session restore")
                    .ConfigureAwait(false);
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
            return await _appCodeAuthorization
                .SignInAsync(instanceUri, cancellationToken)
                .ConfigureAwait(false);
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
                CottonLog.Warning(_logger, "Cotton mobile remote logout failed; clearing local session.", exception);
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
            await TryClearLocalSessionAreaAsync(
                _notificationCursorStore.ClearAsync,
                "notification cursor",
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
                CottonLog.WarningWithContext(
                    _logger,
                    "Failed to clear a Cotton mobile session area.",
                    sessionAreaName,
                    exception);
                failures.Add(exception);
            }
        }

        private static bool IsAuthorizationFailure(CottonApiException exception)
        {
            return exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
        }
    }
}
