namespace Cotton.Mobile.Services
{
    public class CottonNotificationLaunchRequest
    {
        public CottonNotificationLaunchRequest(
            Guid notificationId,
            CottonRemotePushEventCategory category)
        {
            if (notificationId == Guid.Empty)
            {
                throw new ArgumentException("Notification id is required.", nameof(notificationId));
            }

            if (!Enum.IsDefined(category))
            {
                throw new ArgumentOutOfRangeException(nameof(category), "Remote push category is not supported.");
            }

            NotificationId = notificationId;
            Category = category;
        }

        public Guid NotificationId { get; }

        public CottonRemotePushEventCategory Category { get; }
    }
}
