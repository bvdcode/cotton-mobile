namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupMediaAccessDisplayState
    {
        private CottonCameraBackupMediaAccessDisplayState(
            string title,
            string statusText,
            string detailText,
            string actionText,
            bool canReadMedia,
            bool needsAttention,
            bool shouldOpenSettings)
        {
            Title = title;
            StatusText = statusText;
            DetailText = detailText;
            ActionText = actionText;
            CanReadMedia = canReadMedia;
            NeedsAttention = needsAttention;
            ShouldOpenSettings = shouldOpenSettings;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public string ActionText { get; }

        public bool IsActionVisible => !string.IsNullOrWhiteSpace(ActionText);

        public bool CanReadMedia { get; }

        public bool NeedsAttention { get; }

        public bool ShouldOpenSettings { get; }

        public static CottonCameraBackupMediaAccessDisplayState Create(
            CottonCameraBackupMediaAccessState state)
        {
            return state switch
            {
                CottonCameraBackupMediaAccessState.Allowed => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Allowed",
                    "Cotton can scan photos and videos after backup execution is enabled.",
                    actionText: string.Empty,
                    canReadMedia: true,
                    needsAttention: false,
                    shouldOpenSettings: false),
                CottonCameraBackupMediaAccessState.Limited => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Selected media only",
                    "Cotton can use only the media you select until broader access is allowed.",
                    actionText: "Settings",
                    canReadMedia: true,
                    needsAttention: false,
                    shouldOpenSettings: true),
                CottonCameraBackupMediaAccessState.Denied => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Denied",
                    "Allow photo access in Android settings before camera backup can scan media.",
                    actionText: "Settings",
                    canReadMedia: false,
                    needsAttention: true,
                    shouldOpenSettings: true),
                CottonCameraBackupMediaAccessState.Unavailable => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Unavailable",
                    "Media library access is not available on this device.",
                    actionText: string.Empty,
                    canReadMedia: false,
                    needsAttention: true,
                    shouldOpenSettings: false),
                _ => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Not requested",
                    "Cotton will ask before scanning photos or videos.",
                    actionText: "Allow",
                    canReadMedia: false,
                    needsAttention: false,
                    shouldOpenSettings: false),
            };
        }
    }
}
