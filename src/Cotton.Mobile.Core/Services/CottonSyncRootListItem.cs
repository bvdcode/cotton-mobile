namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListItem
    {
        public CottonSyncRootListItem(CottonSyncRootSnapshot root, bool isPaused = false)
        {
            ArgumentNullException.ThrowIfNull(root);

            Id = root.Id;
            Direction = root.Direction;
            Title = root.CloudFolder.FolderName;
            PathText = root.CloudFolder.Path;
            DetailText = $"{CreateDirectionText(root.Direction)} · {root.LocalRoot.DisplayName}";
            IsPaused = isPaused;
            IsUnsupportedLocalRoot = !isPaused && CottonSyncRootRunCapability.HasUnsupportedLocalRoot(root);
            CanRunNow = !isPaused && CottonSyncRootRunCapability.CanRun(root);
            StatusText = CreateStatusText(root, isPaused, IsUnsupportedLocalRoot, CanRunNow);
            IsReady = !isPaused && !IsUnsupportedLocalRoot && CanRunNow;
            IsAttentionVisible = !isPaused
                && (IsUnsupportedLocalRoot || root.NeedsUserAction || !root.CanRunSync || !CanRunNow);
            CanPauseSync = !isPaused;
            CanResumeSync = isPaused;
            CanStopSync = true;
        }

        public Guid Id { get; }

        public CottonSyncDirection Direction { get; }

        public string Title { get; }

        public string PathText { get; }

        public string DetailText { get; }

        public string StatusText { get; }

        public bool IsReady { get; }

        public bool IsAttentionVisible { get; }

        public bool CanRunNow { get; }

        public string RunNowActionText => "Run now";

        public bool IsPaused { get; }

        public bool IsUnsupportedLocalRoot { get; }

        public bool CanPauseSync { get; }

        public string PauseSyncActionText => CottonSyncRootManagementText.PauseAction;

        public bool CanResumeSync { get; }

        public string ResumeSyncActionText => CottonSyncRootManagementText.ResumeAction;

        public bool CanStopSync { get; }

        public string StopSyncActionText => CottonSyncRootManagementText.StopAction;

        private static string CreateStatusText(
            CottonSyncRootSnapshot root,
            bool isPaused,
            bool isUnsupportedLocalRoot,
            bool canRunNow)
        {
            if (isPaused)
            {
                return CottonSyncRootManagementText.PausedStatusText;
            }

            if (isUnsupportedLocalRoot)
            {
                return CottonSyncRootRunCapability.CreateUnsupportedLocalRootStatusText(root);
            }

            return !canRunNow && root.Direction == CottonSyncDirection.Bidirectional && root.CanRunSync
                ? CottonBidirectionalSyncStatusText.ExecutionUnavailableStatus
                : root.StatusText;
        }

        private static string CreateDirectionText(CottonSyncDirection direction)
        {
            return direction switch
            {
                CottonSyncDirection.CloudToDevice => "Cloud to device",
                CottonSyncDirection.DeviceToCloud => "Device to cloud",
                CottonSyncDirection.Bidirectional => "Bidirectional",
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported."),
            };
        }
    }
}
