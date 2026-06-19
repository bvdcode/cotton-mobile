namespace Cotton.Mobile.Services
{
    public sealed class CottonCameraBackupMediaAccessDisplayState
    {
        private CottonCameraBackupMediaAccessDisplayState(
            string title,
            string statusText,
            string detailText,
            bool canReadMedia,
            bool needsAttention)
        {
            Title = title;
            StatusText = statusText;
            DetailText = detailText;
            CanReadMedia = canReadMedia;
            NeedsAttention = needsAttention;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool CanReadMedia { get; }

        public bool NeedsAttention { get; }

        public static CottonCameraBackupMediaAccessDisplayState Create(
            CottonCameraBackupMediaAccessState state)
        {
            return state switch
            {
                CottonCameraBackupMediaAccessState.Allowed => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Allowed",
                    "Cotton can scan photos and videos after backup execution is enabled.",
                    canReadMedia: true,
                    needsAttention: false),
                CottonCameraBackupMediaAccessState.Limited => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Selected media only",
                    "Cotton can use only the media you select until broader access is allowed.",
                    canReadMedia: true,
                    needsAttention: false),
                CottonCameraBackupMediaAccessState.Denied => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Denied",
                    "Allow photo access in Android settings before camera backup can scan media.",
                    canReadMedia: false,
                    needsAttention: true),
                CottonCameraBackupMediaAccessState.Unavailable => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Unavailable",
                    "Media library access is not available on this device.",
                    canReadMedia: false,
                    needsAttention: true),
                _ => new CottonCameraBackupMediaAccessDisplayState(
                    "Media Access",
                    "Not requested",
                    "Cotton will ask before scanning photos or videos.",
                    canReadMedia: false,
                    needsAttention: false),
            };
        }
    }
}
