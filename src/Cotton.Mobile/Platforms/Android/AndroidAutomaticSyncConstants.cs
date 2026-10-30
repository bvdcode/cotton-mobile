// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidAutomaticSyncConstants
    {
        public const string PeriodicWorkName = "cotton.sync.periodic";
        public const string MediaStoreWorkName = "cotton.sync.media-store";
        public const string RootRetryWorkNamePrefix = "cotton.sync.root";
        public const string RootRetryTag = "cotton.sync.root-retry";
        public const string RootIdInputKey = "cotton.sync.root-id";
        public const int PeriodicIntervalMinutes = 15;

        public static string CreateRootRetryWorkName(Guid rootId)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            return $"{RootRetryWorkNamePrefix}.{rootId:D}";
        }
    }
}
#endif
