// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using AndroidX.Activity.Result;

namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidActivityResultCallback(Action<ActivityResult> callback) :
        Java.Lang.Object,
        IActivityResultCallback
    {
        private readonly Action<ActivityResult> _callback =
            callback ?? throw new ArgumentNullException(nameof(callback));

        public void OnActivityResult(Java.Lang.Object? result)
        {
            ActivityResult activityResult = result as ActivityResult
                ?? throw new InvalidDataException("Android activity result is unavailable.");
            _callback(activityResult);
        }
    }
}
#endif
