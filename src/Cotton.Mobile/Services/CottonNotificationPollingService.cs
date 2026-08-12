// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Sdk;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Services
{
    public class CottonNotificationPollingService : ICottonNotificationPollingService, IDisposable
    {
        private const int PageSize = 50;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ICottonSessionService _sessionService;
        private readonly ICottonClientFactory _clientFactory;
        private readonly ICottonNotificationCursorStore _cursorStore;
        private readonly CottonNotificationDeliveryPlanner _deliveryPlanner;
        private readonly ICottonLocalNotificationService _localNotificationService;

        public CottonNotificationPollingService(
            ICottonSessionService sessionService,
            ICottonClientFactory clientFactory,
            ICottonNotificationCursorStore cursorStore,
            CottonNotificationDeliveryPlanner deliveryPlanner,
            ICottonLocalNotificationService localNotificationService)
        {
            ArgumentNullException.ThrowIfNull(sessionService);
            ArgumentNullException.ThrowIfNull(clientFactory);
            ArgumentNullException.ThrowIfNull(cursorStore);
            ArgumentNullException.ThrowIfNull(deliveryPlanner);
            ArgumentNullException.ThrowIfNull(localNotificationService);

            _sessionService = sessionService;
            _clientFactory = clientFactory;
            _cursorStore = cursorStore;
            _deliveryPlanner = deliveryPlanner;
            _localNotificationService = localNotificationService;
        }

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Uri? instanceUri = await _sessionService
                    .GetRememberedSessionInstanceAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (instanceUri is null)
                {
                    return;
                }

                await using ICottonCloudClient client = _clientFactory.Create(instanceUri);
                CottonPagedResult<IReadOnlyList<CottonNotificationDto>> page = await client.Notifications
                    .GetNotificationsAsync(page: 1, pageSize: PageSize, cancellationToken)
                    .ConfigureAwait(false);
                CottonNotificationCursor? cursor = await _cursorStore
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false);
                CottonNotificationDeliveryPlan deliveryPlan = _deliveryPlanner.Create(
                    page.Payload,
                    page.TotalCount,
                    cursor);

                if (deliveryPlan.UnseenCount > 0)
                {
                    await _localNotificationService
                        .ShowAsync(deliveryPlan, cancellationToken)
                        .ConfigureAwait(false);
                }

                await _cursorStore
                    .SaveAsync(deliveryPlan.NextCursor, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            _gate.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
