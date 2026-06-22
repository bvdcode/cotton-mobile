// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Services
{
    public interface IAndroidDeviceCredentialUnlockActivityResultBridge
    {
        Task<CottonDeviceUnlockResult> RequestUnlockAsync(
            Activity activity,
            Intent intent,
            CancellationToken cancellationToken = default);

        bool TryHandleActivityResult(int requestCode, Result resultCode, Intent? data);
    }
}
#endif
