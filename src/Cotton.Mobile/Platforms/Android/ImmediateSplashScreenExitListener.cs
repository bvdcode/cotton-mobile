// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Window;
using System.Runtime.Versioning;

namespace Cotton.Mobile
{
    [SupportedOSPlatform("android31.0")]
    public class ImmediateSplashScreenExitListener :
        Java.Lang.Object,
        ISplashScreenOnExitAnimationListener
    {
        private readonly MainActivity _activity;

        public ImmediateSplashScreenExitListener(MainActivity activity)
        {
            ArgumentNullException.ThrowIfNull(activity);

            _activity = activity;
        }

        public void OnSplashScreenExit(SplashScreenView splashScreenView)
        {
            splashScreenView.Remove();
            _activity.RefreshSystemBars();
        }
    }
}
