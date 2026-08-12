// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Cotton.Mobile.Resources.Localization;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidNotificationChannels
    {
        public static void EnsureCreated()
        {
            if (global::Android.App.Application.Context.GetSystemService(Context.NotificationService)
                is not NotificationManager notificationManager)
            {
                throw new InvalidOperationException("Android notification manager is unavailable.");
            }

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
        }
    }
}
