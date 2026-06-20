using System.Collections.Generic;
using System.Linq;

namespace Cotton.Mobile.Services
{
    public static class CottonRemotePushNotificationFactory
    {
        private const string Title = "Cotton Cloud";

        public static CottonLocalNotificationSnapshot? CreateVisibleNotification(
            IReadOnlyDictionary<string, string> data)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (!TryReadNotificationId(data, out Guid notificationId)
                || !TryReadEventCategory(data, out CottonRemotePushEventCategory category))
            {
                return null;
            }

            CottonLocalNotificationKind kind = GetNotificationKind(category);
            return new CottonLocalNotificationSnapshot(
                CreateNotificationId(notificationId, kind),
                kind,
                GetChannelKind(category),
                Title,
                GetMessage(category),
                new CottonNotificationLaunchRequest(notificationId, category));
        }

        private static bool TryReadNotificationId(
            IReadOnlyDictionary<string, string> data,
            out Guid notificationId)
        {
            notificationId = Guid.Empty;
            return data.TryGetValue(CottonRemotePushMessageDataKeys.NotificationId, out string? value)
                && Guid.TryParse(value, out notificationId)
                && notificationId != Guid.Empty;
        }

        private static bool TryReadEventCategory(
            IReadOnlyDictionary<string, string> data,
            out CottonRemotePushEventCategory category)
        {
            category = default;
            return data.TryGetValue(CottonRemotePushMessageDataKeys.EventCategory, out string? value)
                && Enum.TryParse(value, ignoreCase: false, out category)
                && Enum.IsDefined(category);
        }

        private static CottonNotificationChannelKind GetChannelKind(CottonRemotePushEventCategory category)
        {
            return CottonRemotePushCapabilityCatalog.AndroidClosedTestingCurrentBackend.EventCategories
                .Single(snapshot => snapshot.Category == category)
                .ChannelKind;
        }

        private static CottonLocalNotificationKind GetNotificationKind(CottonRemotePushEventCategory category)
        {
            return category switch
            {
                CottonRemotePushEventCategory.SharedFile => CottonLocalNotificationKind.RemoteSharedFile,
                CottonRemotePushEventCategory.AccessRequest => CottonLocalNotificationKind.RemoteAccessRequest,
                CottonRemotePushEventCategory.CommentMention => CottonLocalNotificationKind.RemoteCommentMention,
                CottonRemotePushEventCategory.SecuritySession => CottonLocalNotificationKind.RemoteSecuritySession,
                _ => throw new ArgumentOutOfRangeException(nameof(category), "Remote push category is not supported."),
            };
        }

        private static string GetMessage(CottonRemotePushEventCategory category)
        {
            return category switch
            {
                CottonRemotePushEventCategory.SharedFile => "Shared-file activity needs attention.",
                CottonRemotePushEventCategory.AccessRequest => "An access request needs attention.",
                CottonRemotePushEventCategory.CommentMention => "A comment or mention needs attention.",
                CottonRemotePushEventCategory.SecuritySession => "Security activity needs attention.",
                _ => throw new ArgumentOutOfRangeException(nameof(category), "Remote push category is not supported."),
            };
        }

        private static int CreateNotificationId(
            Guid notificationId,
            CottonLocalNotificationKind kind)
        {
            int hash = notificationId.GetHashCode() ^ (int)kind;
            int positiveHash = hash & int.MaxValue;
            return positiveHash == 0 ? 1 : positiveHash;
        }
    }
}
