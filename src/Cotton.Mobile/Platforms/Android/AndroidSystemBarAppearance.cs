// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.Content.Res;
using AndroidX.Core.View;
using AndroidX.Fragment.App;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidSystemBarAppearance : FragmentManager.FragmentLifecycleCallbacks
    {
        public override void OnFragmentResumed(FragmentManager fragmentManager, Fragment fragment)
        {
            RefreshDialog(fragment);
        }

        public static void RefreshDialog(Fragment fragment)
        {
            if (fragment is DialogFragment { Dialog.Window: global::Android.Views.Window window }
                && fragment.Resources.Configuration is Configuration configuration)
            {
                ApplyIconColors(window, configuration);
            }
        }

        public static void ApplyIconColors(global::Android.Views.Window window, Configuration configuration)
        {
            bool isLightTheme = (configuration.UiMode & UiMode.NightMask) != UiMode.NightYes;
            WindowInsetsControllerCompat? controller = WindowCompat.GetInsetsController(window, window.DecorView);
            if (controller is not null)
            {
                controller.AppearanceLightStatusBars = isLightTheme;
                controller.AppearanceLightNavigationBars = isLightTheme;
            }
        }
    }
}
