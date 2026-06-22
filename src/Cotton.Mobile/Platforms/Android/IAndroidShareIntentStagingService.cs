// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content;

namespace Cotton.Mobile.Services
{
    public interface IAndroidShareIntentStagingService
    {
        Task<CottonShareIntakeSnapshot?> StageAsync(
            Intent intent,
            ContentResolver? contentResolver,
            CancellationToken cancellationToken = default);
    }
}
