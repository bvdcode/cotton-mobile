// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidAutomaticSyncConstants
    {
        public const string PeriodicWorkName = "cotton.sync.periodic";
        public const string MediaStoreWorkName = "cotton.sync.media-store";
        public const int PeriodicIntervalMinutes = 15;
    }
}
#endif
