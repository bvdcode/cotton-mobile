// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Auth;
using Cotton.Sdk;
using Cotton.Sdk.Auth;

namespace Cotton.Mobile.Services
{
    internal static class CottonAppCodeAuthorizationPoller
    {
        private static readonly TimeSpan MinimumPollDelay = TimeSpan.FromSeconds(1);

        public static async Task<CottonSessionResult> PollUntilCompleteAsync(
            ICottonCloudClient client,
            AppCodeAuthorizationSession session,
            Uri instanceUri,
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);

            while (timeProvider.GetUtcNow().UtcDateTime < session.ExpiresAt)
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
                    timeProvider,
                    cancellationToken).ConfigureAwait(false);
            }

            return CottonSessionResult.FromStatus(CottonSessionResultStatus.TimedOut, instanceUri);
        }

        private static async Task<CottonSessionResult?> TryCreateCompletedResultAsync(
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
                    return CottonSessionResult.FromStatus(
                        CottonSessionResultStatus.AuthorizationDenied,
                        instanceUri,
                        poll.Error);

                case AppCodePollStatus.Expired:
                    return CottonSessionResult.FromStatus(
                        CottonSessionResultStatus.AuthorizationExpired,
                        instanceUri,
                        poll.Error);

                case AppCodePollStatus.NotFound:
                    return CottonSessionResult.FromStatus(
                        CottonSessionResultStatus.AuthorizationNotFound,
                        instanceUri,
                        poll.Error);

                case AppCodePollStatus.Unknown:
                    return CottonSessionResult.FromStatus(
                        CottonSessionResultStatus.AuthorizationFailed,
                        instanceUri,
                        poll.Error);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(poll),
                        poll.Status,
                        "App-code poll status is not supported.");
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
            TimeProvider timeProvider,
            CancellationToken cancellationToken)
        {
            TimeSpan remaining = expiresAt - timeProvider.GetUtcNow().UtcDateTime;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan effectiveDelay = delay < remaining ? delay : remaining;
            await Task.Delay(effectiveDelay, timeProvider, cancellationToken).ConfigureAwait(false);
        }
    }
}
