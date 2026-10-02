// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaStoreSyncHandoff(ICottonAutomaticSyncBackgroundScheduler scheduler)
    {
        public async Task QueueAsync(CancellationToken cancellationToken = default)
        {
            await scheduler.ScheduleMediaStoreSyncAsync(cancellationToken).ConfigureAwait(false);
            await scheduler.RescheduleMediaStoreTriggerAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }
}
