// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Android.Util;
using Cotton.Mobile.Services;
using Firebase.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Cotton.Mobile
{
    [Service(Exported = false)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class AndroidFirebaseMessagingService : FirebaseMessagingService
    {
        private const string LogTag = "CottonPush";

        public override void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            ArgumentNullException.ThrowIfNull(message);

            CottonLocalNotificationSnapshot? notification =
                CottonRemotePushNotificationFactory.CreateVisibleNotification(ReadMessageData(message));
            if (notification is null)
            {
                Log.Info(LogTag, "Skipped Cotton remote push message; payload is not displayable.");
                return;
            }

            ICottonLocalNotificationService? localNotificationService = IPlatformApplication.Current?.Services
                .GetService<ICottonLocalNotificationService>();
            if (localNotificationService is null)
            {
                Log.Warn(LogTag, "Skipped Cotton remote push message; local notification service is unavailable.");
                return;
            }

            _ = ShowRemoteNotificationAsync(localNotificationService, notification);
        }

        public override void OnNewToken(string token)
        {
            base.OnNewToken(token);

            ICottonRemotePushTokenRefreshHandler? handler = IPlatformApplication.Current?.Services
                .GetService<ICottonRemotePushTokenRefreshHandler>();
            if (handler is null)
            {
                Log.Info(LogTag, "Skipped Cotton remote push token refresh; handler is unavailable.");
                return;
            }

            _ = HandleNewTokenAsync(handler, token);
        }

        private static async Task HandleNewTokenAsync(
            ICottonRemotePushTokenRefreshHandler handler,
            string token)
        {
            try
            {
                await handler.HandleNewTokenAsync(token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Failed to handle Cotton remote push token refresh. {exception}");
            }
        }

        private static IReadOnlyDictionary<string, string> ReadMessageData(RemoteMessage message)
        {
            Dictionary<string, string> data = new(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> item in message.Data)
            {
                if (!string.IsNullOrWhiteSpace(item.Key) && item.Value is not null)
                {
                    data[item.Key] = item.Value;
                }
            }

            return data;
        }

        private static async Task ShowRemoteNotificationAsync(
            ICottonLocalNotificationService localNotificationService,
            CottonLocalNotificationSnapshot notification)
        {
            try
            {
                await localNotificationService.ShowAsync(notification).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warn(LogTag, $"Failed to show Cotton remote push notification. {exception}");
            }
        }
    }
}
#endif
