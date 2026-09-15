// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Android.App;
using Android.Content;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Platforms.Android
{
    internal static class AndroidNotificationBuilder
    {
        public static Notification Build(
            Context context,
            int notificationId,
            string title,
            string? message,
            CottonNotificationPriority priority)
        {
            string channelId = ResolveChannelId(priority);
            Notification.Builder builder = new(context, channelId);

            builder
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.ic_stat_cotton_cloud)
                .SetColor(context.GetColor(Resource.Color.cotton_accent))
                .SetAutoCancel(true)
                .SetShowWhen(true)
                .SetOnlyAlertOnce(true)
                .SetGroup(AndroidNotificationConstants.GroupKey)
                .SetCategory(Notification.CategoryMessage);

            if (!string.IsNullOrWhiteSpace(message))
            {
                builder.SetContentText(message);
                builder.SetStyle(new Notification.BigTextStyle().BigText(message));
            }

            PendingIntent? launchIntent = CreateLaunchIntent(context, notificationId);
            if (launchIntent is not null)
            {
                builder.SetContentIntent(launchIntent);
            }

            return builder.Build();
        }

        private static PendingIntent? CreateLaunchIntent(Context context, int notificationId)
        {
            Intent? launchIntent = context.PackageManager?.GetLaunchIntentForPackage(
                context.PackageName ?? string.Empty);
            if (launchIntent is null)
            {
                return null;
            }

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;

            return PendingIntent.GetActivity(context, notificationId, launchIntent, flags);
        }

        private static string ResolveChannelId(CottonNotificationPriority priority)
        {
            return priority switch
            {
                CottonNotificationPriority.None => AndroidNotificationConstants.GeneralChannelId,
                CottonNotificationPriority.Low => AndroidNotificationConstants.GeneralChannelId,
                CottonNotificationPriority.Medium => AndroidNotificationConstants.GeneralChannelId,
                CottonNotificationPriority.High => AndroidNotificationConstants.SecurityChannelId,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown notification priority."),
            };
        }
    }
}
