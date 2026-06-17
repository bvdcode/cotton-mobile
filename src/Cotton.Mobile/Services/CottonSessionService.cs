using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public class CottonSessionService : ICottonSessionService
    {
        private static readonly TimeSpan MinimumPollDelay = TimeSpan.FromSeconds(1);

        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonInstanceStore _instanceStore;
        private readonly ICottonMobileApplicationMetadata _metadata;
        private readonly ICottonTokenStore _tokenStore;
        private readonly IBrowser _browser;
        private readonly IApplicationForegroundService _foregroundService;

        public CottonSessionService(
            ICottonClientFactory clientFactory,
            ICottonInstanceStore instanceStore,
            ICottonMobileApplicationMetadata metadata,
            ICottonTokenStore tokenStore,
            IBrowser browser,
            IApplicationForegroundService foregroundService)
        {
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(instanceStore);
            ArgumentNullException.ThrowIfNull(metadata);
            ArgumentNullException.ThrowIfNull(tokenStore);
            ArgumentNullException.ThrowIfNull(browser);
            ArgumentNullException.ThrowIfNull(foregroundService);

            _clientFactory = clientFactory;
            _instanceStore = instanceStore;
            _metadata = metadata;
            _tokenStore = tokenStore;
            _browser = browser;
            _foregroundService = foregroundService;
        }

        public async Task<CottonSessionResult> RestoreAsync(CancellationToken cancellationToken = default)
        {
            Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (instanceUri is null)
            {
                return CottonSessionResult.Unauthenticated();
            }

            TokenPairDto? tokens = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (tokens is null)
            {
                return CottonSessionResult.Unauthenticated(instanceUri);
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await client.Auth.RefreshAsync(tokens.RefreshToken, cancellationToken).ConfigureAwait(false);
            UserDto user = await client.Auth.MeAsync(cancellationToken).ConfigureAwait(false);

            return CottonSessionResult.Authenticated(instanceUri, user);
        }

        public async Task<CottonSessionResult> SignInWithBrowserAsync(
            Uri instanceUri,
            CancellationToken cancellationToken = default)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            await _tokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
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

            bool browserOpened = await MainThread.InvokeOnMainThreadAsync(
                () => _browser.OpenAsync(session.ApprovalUri, CottonBrowserLaunchOptions.SystemPreferred()))
                .ConfigureAwait(false);
            if (!browserOpened)
            {
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.BrowserUnavailable, instanceUri);
            }

            bool returnedBeforeExpiration = await WaitForBrowserReturnAsync(
                session,
                cancellationToken).ConfigureAwait(false);
            if (!returnedBeforeExpiration)
            {
                return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
            }

            return await PollUntilCompleteAsync(client, session, instanceUri, cancellationToken).ConfigureAwait(false);
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            Uri? instanceUri = await _instanceStore.GetAsync(cancellationToken).ConfigureAwait(false);
            if (instanceUri is null)
            {
                await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            await client.Auth.LogoutAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            await ClearLocalSessionAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ClearLocalSessionAsync(CancellationToken cancellationToken = default)
        {
            await _tokenStore.ClearAsync(cancellationToken).ConfigureAwait(false);
            await _instanceStore.ClearAsync(cancellationToken).ConfigureAwait(false);
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
                await _foregroundService.WaitForNextResumeAsync(timeout.Token).ConfigureAwait(false);
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
    }
}
