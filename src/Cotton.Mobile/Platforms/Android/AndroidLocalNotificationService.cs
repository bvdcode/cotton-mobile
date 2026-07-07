// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;

namespace Cotton.Mobile.Services
{
    public class AndroidLocalNotificationService : ICottonLocalNotificationService
    {
        private readonly ICottonNotificationPermissionService _permissionService;
        private readonly ICottonNotificationChannelProvisioningService _channelProvisioningService;

        public AndroidLocalNotificationService(
            ICottonNotificationPermissionService permissionService,
            ICottonNotificationChannelProvisioningService channelProvisioningService)
        {
            ArgumentNullException.ThrowIfNull(permissionService);
            ArgumentNullException.ThrowIfNull(channelProvisioningService);

            _permissionService = permissionService;
            _channelProvisioningService = channelProvisioningService;
        }

        public async Task<CottonLocalNotificationDeliveryResult> ShowAsync(
            CottonLocalNotificationSnapshot notification,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(notification);
            cancellationToken.ThrowIfCancellationRequested();

            CottonNotificationPermissionState permissionState = await _permissionService
                .GetPermissionStateAsync(cancellationToken)
                .ConfigureAwait(false);
            if (permissionState is not CottonNotificationPermissionState.Allowed
                and not CottonNotificationPermissionState.NotRequired)
            {
                return CottonLocalNotificationDeliveryResult.Skipped;
            }

            _channelProvisioningService.EnsureChannels();

            Context context = Android.App.Application.Context;
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager notificationManager)
            {
                return CottonLocalNotificationDeliveryResult.Skipped;
            }

            Notification notificationPayload = BuildNotification(context, notification);
            notificationManager.Notify(notification.Id, notificationPayload);
            return CottonLocalNotificationDeliveryResult.Posted;
        }

        private static Notification BuildNotification(
            Context context,
            CottonLocalNotificationSnapshot notification)
        {
            CottonNotificationChannelSnapshot channel =
                CottonNotificationChannelCatalog.Get(notification.ChannelKind);

            Notification.Builder builder;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
#pragma warning disable CA1416
                builder = new Notification.Builder(context, channel.Id);
#pragma warning restore CA1416
            }
            else
            {
#pragma warning disable CA1422
                builder = new Notification.Builder(context);
#pragma warning restore CA1422
            }

            Intent? launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
            PendingIntentFlags flags = PendingIntentFlags.UpdateCurrent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
#pragma warning disable CA1416
                flags |= PendingIntentFlags.Immutable;
#pragma warning restore CA1416
            }

            PendingIntent? pendingIntent = launchIntent is null
                ? null
                : PendingIntent.GetActivity(
                    context,
                    notification.Id,
                    AddLaunchExtras(launchIntent, notification),
                    flags);

            builder
                .SetContentTitle(notification.Title)
                .SetContentText(notification.Message)
                .SetSmallIcon(Resource.Drawable.ic_stat_cotton_cloud)
                .SetColor(context.GetColor(Resource.Color.cotton_accent))
                .SetAutoCancel(true)
                .SetShowWhen(true)
                .SetCategory(Notification.CategoryStatus);

            if (pendingIntent is not null)
            {
                builder.SetContentIntent(pendingIntent);
            }

            return builder.Build();
        }

        private static Intent AddLaunchExtras(
            Intent launchIntent,
            CottonLocalNotificationSnapshot notification)
        {
            if (notification.LaunchRequest is null)
            {
                return launchIntent;
            }

            launchIntent.PutExtra(AndroidNotificationIntentExtras.IsNotificationLaunch, true);
            launchIntent.PutExtra(
                AndroidNotificationIntentExtras.NotificationId,
                notification.LaunchRequest.NotificationId.ToString("D"));
            launchIntent.PutExtra(
                AndroidNotificationIntentExtras.EventCategory,
                notification.LaunchRequest.Category.ToString());
            return launchIntent;
        }
    }
}
#endif
