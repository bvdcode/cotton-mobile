// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonNotificationPollingService(
        ICottonNotificationBatchProvider batchProvider,
        ICottonNotificationCursorStore cursorStore,
        CottonNotificationDeliveryPlanner deliveryPlanner,
        ICottonLocalNotificationService localNotificationService) :
        ICottonNotificationPollingService,
        IDisposable
    {
        private const int DetailLimit = 50;

        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly ICottonNotificationBatchProvider _batchProvider =
            batchProvider ?? throw new ArgumentNullException(nameof(batchProvider));
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
                CottonNotificationCursor? cursor = await _cursorStore
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false);
                CottonNotificationBatch? batch = await _batchProvider
                    .GetAsync(cursor, DetailLimit, cancellationToken)
                    .ConfigureAwait(false);
                if (batch is null)
                {
                    return;
                }

                CottonNotificationDeliveryPlan deliveryPlan = _deliveryPlanner.Create(batch);

                if (deliveryPlan.UnreadCount == 0)
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
            return deliveryPlan.NextCursor is null
                ? Task.CompletedTask
                : _cursorStore.SaveAsync(deliveryPlan.NextCursor, cancellationToken);
        }
    }
}
