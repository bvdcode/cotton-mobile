// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonSdkNotificationBatchProvider(
        ICottonSessionService sessionService,
        ICottonClientFactory clientFactory) : ICottonNotificationBatchProvider
    {
        private readonly ICottonSessionService _sessionService =
            sessionService ?? throw new ArgumentNullException(nameof(sessionService));
        private readonly ICottonClientFactory _clientFactory =
            clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));

        public async Task<CottonNotificationBatch?> GetAsync(
            CottonNotificationCursor? cursor,
            int detailLimit,
            CancellationToken cancellationToken = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(detailLimit);

            Uri? instanceUri = await _sessionService
                .GetRememberedSessionInstanceAsync(cancellationToken)
                .ConfigureAwait(false);
            if (instanceUri is null)
            {
                return null;
            }

            CottonNotificationCursorDto? sdkCursor = CreateSdkCursor(cursor);
            await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
            CottonNotificationBatchDto batch = await client.Notifications
                .GetNotificationBatchAsync(sdkCursor, detailLimit, cancellationToken)
                .ConfigureAwait(false);
            return new CottonNotificationBatch(
                batch.UnreadNotifications,
                batch.UnreadCount,
                CreateDomainCursor(batch.NextCursor));
        }

        private static CottonNotificationCursorDto? CreateSdkCursor(CottonNotificationCursor? cursor)
        {
            if (cursor is null)
            {
                return null;
            }

            return new CottonNotificationCursorDto
            {
                CreatedAt = cursor.CreatedAt,
                NotificationId = cursor.NotificationId,
            };
        }

        private static CottonNotificationCursor? CreateDomainCursor(CottonNotificationCursorDto? cursor)
        {
            return cursor is null
                ? null
                : new CottonNotificationCursor(cursor.CreatedAt, cursor.NotificationId);
        }
    }
}
