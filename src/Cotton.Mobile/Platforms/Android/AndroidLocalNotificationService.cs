// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Buffers.Binary;
using Android.App;
using Android.Content;
using Android.OS;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile
{
    public class AndroidLocalNotificationService : ICottonLocalNotificationService
    {
        private readonly ICottonNotificationPermissionService _permissionService;
        private readonly AndroidNotificationChannelService _channelService;

        public AndroidLocalNotificationService(
            ICottonNotificationPermissionService permissionService,
            AndroidNotificationChannelService channelService)
        {
            ArgumentNullException.ThrowIfNull(permissionService);
            ArgumentNullException.ThrowIfNull(channelService);

            _permissionService = permissionService;
            _channelService = channelService;
        }

        public async Task ShowAsync(
            CottonNotificationDeliveryPlan deliveryPlan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(deliveryPlan);
            cancellationToken.ThrowIfCancellationRequested();

            if (!await _permissionService.CanPostAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            _channelService.EnsureChannels();
            Context context = Android.App.Application.Context;
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            {
                throw new InvalidOperationException("Android notification manager is unavailable.");
            }

            if (deliveryPlan.IsSummary)
            {
                CottonNotificationDto latest = deliveryPlan.Notifications[0];
                string message = AppResources.CreateNotificationSummary(
                    latest.Title,
                    deliveryPlan.UnseenCount - 1);
                manager.Notify(
                    AndroidNotificationConstants.SummaryNotificationId,
                    BuildNotification(
                        context,
                        AndroidNotificationConstants.SummaryNotificationId,
                        AppResources.AppTitle,
                        message,
                        latest.Priority));
                return;
            }

            foreach (CottonNotificationDto notification in deliveryPlan.Notifications.Reverse())
            {
                int notificationId = CreateNotificationId(notification.Id);
                manager.Notify(
                    notificationId,
                    BuildNotification(
                        context,
                        notificationId,
                        notification.Title,
                        notification.Content,
                        notification.Priority));
            }
        }

        private static Notification BuildNotification(
            Context context,
            int notificationId,
            string title,
            string? message,
            CottonNotificationPriority priority)
        {
            string channelId = ResolveChannelId(priority);
            Notification.Builder builder;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                builder = new Notification.Builder(context, channelId);
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CA1422
                builder = new Notification.Builder(context);
#pragma warning restore CA1422
            }

            builder
                .SetContentTitle(title)
                .SetSmallIcon(Resource.Drawable.ic_stat_cotton_cloud)
                .SetColor(context.GetColor(Resource.Color.cotton_accent))
                .SetAutoCancel(true)
                .SetShowWhen(true)
                .SetGroup(AndroidNotificationConstants.GroupKey)
                .SetCategory(Notification.CategoryMessage);

            if (Build.VERSION.SdkInt < BuildVersionCodes.O)
            {
#pragma warning disable CA1422
                builder.SetPriority(ResolveAndroidPriority(priority));
#pragma warning restore CA1422
            }

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

            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
#pragma warning disable CA1416
                flags |= PendingIntentFlags.Immutable;
#pragma warning restore CA1416
            }

            return PendingIntent.GetActivity(context, notificationId, launchIntent, flags);
        }

        private static int CreateNotificationId(Guid notificationId)
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(notificationId.ToByteArray()) & int.MaxValue;
            return value == 0 ? 1 : value;
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

        private static int ResolveAndroidPriority(CottonNotificationPriority priority)
        {
            return priority switch
            {
                CottonNotificationPriority.None => (int)NotificationPriority.Default,
                CottonNotificationPriority.Low => (int)NotificationPriority.Low,
                CottonNotificationPriority.Medium => (int)NotificationPriority.Default,
                CottonNotificationPriority.High => (int)NotificationPriority.High,
                _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown notification priority."),
            };
        }
    }
}
