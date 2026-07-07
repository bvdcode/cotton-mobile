// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;

namespace Cotton.Mobile.Services
{
    public class AndroidNotificationChannelProvisioningService
        : ICottonNotificationChannelProvisioningService
    {
        public void EnsureChannels()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
                return;
            }

            if (Android.App.Application.Context.GetSystemService(Context.NotificationService)
                is not NotificationManager notificationManager)
            {
                return;
            }

#pragma warning disable CA1416
            foreach (CottonNotificationChannelSnapshot channel in CottonNotificationChannelCatalog.All)
            {
                var androidChannel = new NotificationChannel(
                    channel.Id,
                    channel.Name,
                    MapImportance(channel.Importance))
                {
                    Description = channel.Description,
                };

                notificationManager.CreateNotificationChannel(androidChannel);
            }
#pragma warning restore CA1416
        }

#pragma warning disable CA1416
        private static NotificationImportance MapImportance(CottonNotificationImportance importance)
        {
            return importance switch
            {
                CottonNotificationImportance.High => NotificationImportance.High,
                CottonNotificationImportance.Low => NotificationImportance.Low,
                _ => NotificationImportance.Default,
            };
        }
#pragma warning restore CA1416
    }
}
#endif
