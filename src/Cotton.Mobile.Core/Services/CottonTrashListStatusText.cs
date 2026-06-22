// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonTrashListStatusText
    {
        public const string CancelledStatus = "Trash refresh cancelled.";

        public const string FailedStatus = "Could not load trash.";

        public const string LoadingStatus = "Loading trash...";

        public const string OfflineUnavailableStatus = "Offline. Trash needs internet.";

        public static string CreateLoadedStatus(int itemCount)
        {
            return CottonTrashListSnapshot.CreateSummaryText(itemCount);
        }
    }
}
