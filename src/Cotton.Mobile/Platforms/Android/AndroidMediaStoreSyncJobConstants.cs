// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaStoreSyncJobConstants
    {
        public const int JobId = 0x43544D01;
        public const string ServicePermission = "android.permission.BIND_JOB_SERVICE";
        public const long TriggerUpdateDelayMilliseconds = 5_000;
        public const long TriggerMaximumDelayMilliseconds = 30_000;
    }
}
#endif
