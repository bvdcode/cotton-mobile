namespace Cotton.Mobile.Services
{
    public sealed class CottonNotificationPermissionDisplayState
    {
        private CottonNotificationPermissionDisplayState(
            string title,
            string statusText,
            string detailText,
            string actionText,
            bool canSendNotifications,
            bool needsAttention,
            bool shouldOpenSettings)
        {
            Title = title;
            StatusText = statusText;
            DetailText = detailText;
            ActionText = actionText;
            CanSendNotifications = canSendNotifications;
            NeedsAttention = needsAttention;
            ShouldOpenSettings = shouldOpenSettings;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public string ActionText { get; }

        public bool IsActionVisible => !string.IsNullOrWhiteSpace(ActionText);

        public bool CanSendNotifications { get; }

        public bool NeedsAttention { get; }

        public bool ShouldOpenSettings { get; }

        public static CottonNotificationPermissionDisplayState Create(
            CottonNotificationSettings settings,
            CottonNotificationPermissionState permissionState)
        {
            if (!settings.HasEnabledChannels)
            {
                return new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Off",
                    "All notification categories are off.",
                    actionText: string.Empty,
                    canSendNotifications: false,
                    needsAttention: false,
                    shouldOpenSettings: false);
            }

            return permissionState switch
            {
                CottonNotificationPermissionState.NotRequired => new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Allowed",
                    "Android does not require notification permission on this device.",
                    actionText: string.Empty,
                    canSendNotifications: true,
                    needsAttention: false,
                    shouldOpenSettings: false),
                CottonNotificationPermissionState.Allowed => new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Allowed",
                    "Cotton can send enabled transfer, backup, share, and security notifications.",
                    actionText: string.Empty,
                    canSendNotifications: true,
                    needsAttention: false,
                    shouldOpenSettings: false),
                CottonNotificationPermissionState.Denied => new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Denied",
                    "Notifications are off. Transfer and backup alerts stay in the app.",
                    actionText: "Settings",
                    canSendNotifications: false,
                    needsAttention: true,
                    shouldOpenSettings: true),
                CottonNotificationPermissionState.Unavailable => new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Unavailable",
                    "Notifications are not available on this device.",
                    actionText: string.Empty,
                    canSendNotifications: false,
                    needsAttention: true,
                    shouldOpenSettings: false),
                _ => new CottonNotificationPermissionDisplayState(
                    "Notifications",
                    "Not requested",
                    "Cotton will ask before sending transfer or backup notifications.",
                    actionText: "Allow",
                    canSendNotifications: false,
                    needsAttention: false,
                    shouldOpenSettings: false),
            };
        }
    }
}
