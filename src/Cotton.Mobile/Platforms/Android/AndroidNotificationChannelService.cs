// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Android.OS;
using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidNotificationChannelService
    {
        public void EnsureChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            if (global::Android.App.Application.Context.GetSystemService(Context.NotificationService)
                is not NotificationManager notificationManager)
            {
                throw new InvalidOperationException("Android notification manager is unavailable.");
            }

#pragma warning disable CA1416
            NotificationChannel generalChannel = new(
                AndroidNotificationConstants.GeneralChannelId,
                AppResources.NotificationChannelName,
                NotificationImportance.Default)
            {
                Description = AppResources.NotificationChannelDescription,
            };
            NotificationChannel securityChannel = new(
                AndroidNotificationConstants.SecurityChannelId,
                AppResources.SecurityNotificationChannelName,
                NotificationImportance.High)
            {
                Description = AppResources.SecurityNotificationChannelDescription,
            };

            notificationManager.CreateNotificationChannel(generalChannel);
            notificationManager.CreateNotificationChannel(securityChannel);
#pragma warning restore CA1416
        }
    }
}
