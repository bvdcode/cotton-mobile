// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.Content.PM;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaReadAccessResolver
    {
        public static AndroidMediaReadAccessSnapshot Resolve()
        {
            global::Android.Content.Context context = global::Android.App.Application.Context;
            if (OperatingSystem.IsAndroidVersionAtLeast(34))
            {
                return new AndroidMediaReadAccessSnapshot(
                    IsGranted(context, global::Android.Manifest.Permission.ReadMediaImages),
                    IsGranted(context, global::Android.Manifest.Permission.ReadMediaVideo),
                    IsGranted(context, global::Android.Manifest.Permission.ReadMediaVisualUserSelected));
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                return new AndroidMediaReadAccessSnapshot(
                    IsGranted(context, global::Android.Manifest.Permission.ReadMediaImages),
                    IsGranted(context, global::Android.Manifest.Permission.ReadMediaVideo));
            }

            bool hasLegacyAccess = IsGranted(
                context,
                global::Android.Manifest.Permission.ReadExternalStorage);
            return new AndroidMediaReadAccessSnapshot(hasLegacyAccess, hasLegacyAccess);
        }

        private static bool IsGranted(global::Android.Content.Context context, string permission)
        {
            return context.CheckSelfPermission(permission) == Permission.Granted;
        }
    }
}
#endif
