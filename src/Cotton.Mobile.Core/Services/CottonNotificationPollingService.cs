// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationPollingService(
        ICottonNotificationPageProvider pageProvider,
        ICottonNotificationCursorStore cursorStore,
        CottonNotificationDeliveryPlanner deliveryPlanner,
        ICottonLocalNotificationService localNotificationService) :
        ICottonNotificationPollingService,
        IDisposable
    {
        private const int PageSize = 50;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ICottonNotificationPageProvider _pageProvider =
            pageProvider ?? throw new ArgumentNullException(nameof(pageProvider));
        private readonly ICottonNotificationCursorStore _cursorStore =
            cursorStore ?? throw new ArgumentNullException(nameof(cursorStore));
        private readonly CottonNotificationDeliveryPlanner _deliveryPlanner =
            deliveryPlanner ?? throw new ArgumentNullException(nameof(deliveryPlanner));
        private readonly ICottonLocalNotificationService _localNotificationService =
            localNotificationService ?? throw new ArgumentNullException(nameof(localNotificationService));

        public async Task CheckAsync(CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                CottonNotificationPage? page = await _pageProvider
                    .GetLatestAsync(PageSize, cancellationToken)
                    .ConfigureAwait(false);
                if (page is null)
                {
                    return;
                }

                CottonNotificationCursor? cursor = await _cursorStore
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false);
                CottonNotificationDeliveryPlan deliveryPlan = _deliveryPlanner.Create(
                    page.Notifications,
                    page.TotalCount,
                    cursor);

                if (deliveryPlan.UnseenCount == 0)
                {
                    await SaveCursorAsync(deliveryPlan, cancellationToken).ConfigureAwait(false);
                    return;
                }

                CottonLocalNotificationDeliveryStatus deliveryStatus = await _localNotificationService
                    .ShowAsync(deliveryPlan, cancellationToken)
                    .ConfigureAwait(false);
                if (deliveryStatus == CottonLocalNotificationDeliveryStatus.Delivered)
                {
                    await SaveCursorAsync(deliveryPlan, cancellationToken).ConfigureAwait(false);
                }
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

        private Task SaveCursorAsync(
            CottonNotificationDeliveryPlan deliveryPlan,
            CancellationToken cancellationToken)
        {
            return _cursorStore.SaveAsync(deliveryPlan.NextCursor, cancellationToken);
        }
    }
}
