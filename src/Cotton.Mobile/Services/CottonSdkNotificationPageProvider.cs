// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonSdkNotificationPageProvider(
        ICottonSessionService sessionService,
        ICottonClientFactory clientFactory) : ICottonNotificationPageProvider
    {
        private readonly ICottonSessionService _sessionService =
            sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        private readonly ICottonClientFactory _clientFactory =
            clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        public async Task<CottonNotificationPage?> GetLatestAsync(
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

            Uri? instanceUri = await _sessionService
                .GetRememberedSessionInstanceAsync(cancellationToken)
                .ConfigureAwait(false);
            if (instanceUri is null)
            {
                return null;
            }

            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            CottonPagedResult<IReadOnlyList<CottonNotificationDto>> page = await client.Notifications
                .GetNotificationsAsync(page: 1, pageSize, cancellationToken)
                .ConfigureAwait(false);
            return new CottonNotificationPage(page.Payload, page.TotalCount);
        }
    }
}
