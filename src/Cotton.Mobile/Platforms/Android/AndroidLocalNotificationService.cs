// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Buffers.Binary;
using Android.App;
using Android.Content;
using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.Services;
using Cotton.Sdk.Notifications;

namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidLocalNotificationService : ICottonLocalNotificationService
    {
        private readonly ICottonNotificationPermissionService _permissionService;

        public AndroidLocalNotificationService(ICottonNotificationPermissionService permissionService)
        {
            ArgumentNullException.ThrowIfNull(permissionService);

            _permissionService = permissionService;
        }

        public async Task<CottonLocalNotificationDeliveryStatus> ShowAsync(
            CottonNotificationDeliveryPlan deliveryPlan,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(deliveryPlan);
            cancellationToken.ThrowIfCancellationRequested();

            if (!await _permissionService.CanPostAsync(cancellationToken).ConfigureAwait(false))
            {
                return CottonLocalNotificationDeliveryStatus.PermissionDenied;
            }

            AndroidNotificationChannels.EnsureCreated();
            Context context = global::Android.App.Application.Context;
            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
            {
                throw new InvalidOperationException("Android notification manager is unavailable.");
            }

            if (deliveryPlan.IsSummary)
            {
                CottonNotificationDto latest = deliveryPlan.Notifications[0];
                string message = AppResources.CreateNotificationSummary(
                    latest.Title,
                    deliveryPlan.UnreadCount - 1);
                manager.Notify(
                    AndroidNotificationConstants.SummaryNotificationId,
                    AndroidNotificationBuilder.Build(
                        context,
                        AndroidNotificationConstants.SummaryNotificationId,
                        AppResources.AppTitle,
                        message,
                        latest.Priority));
                return CottonLocalNotificationDeliveryStatus.Delivered;
            }

            foreach (CottonNotificationDto notification in deliveryPlan.Notifications.Reverse())
            {
                int notificationId = CreateNotificationId(notification.Id);
                manager.Notify(
                    notificationId,
                    AndroidNotificationBuilder.Build(
                        context,
                        notificationId,
                        notification.Title,
                        notification.Content,
                        notification.Priority));
            }

            return CottonLocalNotificationDeliveryStatus.Delivered;
        }

        private static int CreateNotificationId(Guid notificationId)
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(notificationId.ToByteArray()) & int.MaxValue;
            return value == 0 ? 1 : value;
        }

    }
}
