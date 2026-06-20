using System.Globalization;

namespace Cotton.Mobile.Services
{
    public class CottonPermissionLedgerDisplayState
    {
        private CottonPermissionLedgerDisplayState(
            IReadOnlyList<CottonPermissionLedgerItem> items,
            string statusText,
            string detailText)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentException.ThrowIfNullOrWhiteSpace(statusText);
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            Items = items;
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
        }

        public string Title => "Permission ledger";

        public IReadOnlyList<CottonPermissionLedgerItem> Items { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool HasItems => Items.Count > 0;

        public bool HasAttention => Items.Any(item => item.NeedsAttention);

        public static CottonPermissionLedgerDisplayState Create(
            CottonNotificationPermissionState notificationPermission,
            CottonCameraBackupMediaAccessState mediaAccess,
            CottonAppLockSettings appLockSettings,
            CottonAppLockCapabilitySnapshot appLockCapability,
            CottonDeviceUnlockAvailabilitySnapshot deviceUnlockAvailability)
        {
            ArgumentNullException.ThrowIfNull(appLockSettings);
            ArgumentNullException.ThrowIfNull(appLockCapability);
            ArgumentNullException.ThrowIfNull(deviceUnlockAvailability);

            CottonPermissionLedgerItem[] items =
            [
                CreateNotificationItem(notificationPermission),
                CreateMediaAccessItem(mediaAccess),
                CreateAppLockItem(appLockSettings, appLockCapability, deviceUnlockAvailability),
                new CottonPermissionLedgerItem(
                    "Selected files",
                    "Scoped",
                    "Manual uploads and shared items use only files selected or shared by the user.",
                    needsAttention: false),
                new CottonPermissionLedgerItem(
                    "Document scan",
                    "System scanner",
                    "Scans are returned by Android's document scanner; Cotton does not request camera permission.",
                    needsAttention: false),
                new CottonPermissionLedgerItem(
                    "Network",
                    "Allowed",
                    "Internet and network-state access are used to connect to Cotton and show offline state.",
                    needsAttention: false),
            ];

            return new CottonPermissionLedgerDisplayState(
                items,
                CreateStatusText(items),
                "Current device access used by Cotton features.");
        }

        public static CottonPermissionLedgerDisplayState Unavailable(string detailText)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            return new CottonPermissionLedgerDisplayState(
                [
                    new CottonPermissionLedgerItem(
                        "Device access",
                        "Unavailable",
                        detailText,
                        needsAttention: true),
                ],
                "Needs review",
                detailText);
        }

        private static CottonPermissionLedgerItem CreateNotificationItem(
            CottonNotificationPermissionState notificationPermission)
        {
            CottonNotificationPermissionDisplayState display =
                CottonNotificationPermissionDisplayState.Create(
                    CottonNotificationSettings.Default,
                    notificationPermission);
            return new CottonPermissionLedgerItem(
                display.Title,
                display.StatusText,
                display.DetailText,
                display.NeedsAttention);
        }

        private static CottonPermissionLedgerItem CreateMediaAccessItem(
            CottonCameraBackupMediaAccessState mediaAccess)
        {
            CottonCameraBackupMediaAccessDisplayState display =
                CottonCameraBackupMediaAccessDisplayState.Create(mediaAccess);
            return new CottonPermissionLedgerItem(
                "Photos and videos",
                display.StatusText,
                display.DetailText,
                display.NeedsAttention);
        }

        private static CottonPermissionLedgerItem CreateAppLockItem(
            CottonAppLockSettings appLockSettings,
            CottonAppLockCapabilitySnapshot appLockCapability,
            CottonDeviceUnlockAvailabilitySnapshot deviceUnlockAvailability)
        {
            if (appLockSettings.IsEnabled && appLockCapability.CanEnable)
            {
                return new CottonPermissionLedgerItem(
                    "Device lock",
                    "Protected",
                    "App lock uses the device screen lock after Cotton is in the background.",
                    needsAttention: false);
            }

            if (appLockCapability.CanEnable && deviceUnlockAvailability.CanVerify)
            {
                return new CottonPermissionLedgerItem(
                    "Device lock",
                    "Available",
                    "Cotton can require device unlock when App lock is turned on.",
                    needsAttention: false);
            }

            return new CottonPermissionLedgerItem(
                "Device lock",
                "Unavailable",
                appLockCapability.DetailText,
                needsAttention: true);
        }

        private static string CreateStatusText(IReadOnlyCollection<CottonPermissionLedgerItem> items)
        {
            int attentionCount = items.Count(item => item.NeedsAttention);
            return attentionCount == 0
                ? "All clear"
                : attentionCount.ToString("N0", CultureInfo.InvariantCulture)
                    + (attentionCount == 1 ? " review" : " reviews");
        }
    }
}
