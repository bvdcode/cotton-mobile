// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    public class RecordingCottonLocalNotificationService(
        CottonLocalNotificationDeliveryStatus deliveryStatus) : ICottonLocalNotificationService
    {
        public int CallCount { get; private set; }

        public CottonNotificationDeliveryPlan? LastDeliveryPlan { get; private set; }

        public Task<CottonLocalNotificationDeliveryStatus> ShowAsync(
            CottonNotificationDeliveryPlan deliveryPlan,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            LastDeliveryPlan = deliveryPlan;
            return Task.FromResult(deliveryStatus);
        }
    }
}
