// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.OS;

namespace Cotton.Mobile.Services
{
    public sealed class AndroidApiLevelProvider : IAndroidApiLevelProvider
    {
        public int CurrentApiLevel => (int)Build.VERSION.SdkInt;
    }
}
#endif
