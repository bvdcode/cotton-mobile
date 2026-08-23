// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;

namespace Cotton.Mobile.Platforms.Android
{
    public interface IAndroidDocumentTreeActivityResultBridge
    {
        Task<Intent?> StartOpenDocumentTreeAsync(
            Activity activity,
            Intent intent,
            Guid requestId,
            CancellationToken cancellationToken = default);

        void CompleteRequest(Guid requestId);

        void HandleActivityResult(Result resultCode, Intent? data);
    }
}
#endif
