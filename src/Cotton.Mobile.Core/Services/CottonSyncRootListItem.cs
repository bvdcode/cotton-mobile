// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

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
            CanReconnect = root.LocalRoot.RequiresPersistedUserGrant && root.NeedsUserAction;
            CanUsePrimaryAction = CanReconnect || CanRunNow;
            PrimaryActionText = CreatePrimaryActionText(root, CanReconnect, CanRunNow);
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

        public bool CanReconnect { get; }

        public bool CanUsePrimaryAction { get; }

        public string PrimaryActionText { get; }

        public bool IsPaused { get; }

        public bool IsUnsupportedLocalRoot { get; }

        public bool CanPauseSync { get; }

        public string PauseSyncActionText => CottonSyncRootManagementText.PauseAction;

        public bool CanResumeSync { get; }

        public string ResumeSyncActionText => CottonSyncRootManagementText.ResumeAction;

        public bool CanStopSync { get; }

        public string StopSyncActionText => CottonSyncRootManagementText.StopAction;

        private static string CreatePrimaryActionText(
            CottonSyncRootSnapshot root,
            bool canReconnect,
            bool canRunNow)
        {
            if (canReconnect)
            {
                return root.StatusText;
            }

            if (canRunNow)
            {
                return "Run now";
            }

            return string.Empty;
        }

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
                return "Unsupported";
            }

            if (!canRunNow && root.Direction == CottonSyncDirection.Bidirectional && root.CanRunSync)
            {
                return "Unavailable";
            }

            CottonSyncRootReadinessStatus readinessStatus = root.ReadinessStatus;
            return readinessStatus switch
            {
                CottonSyncRootReadinessStatus.Ready => "Ready",
                CottonSyncRootReadinessStatus.NeedsUserGrant => "Choose folder",
                CottonSyncRootReadinessStatus.GrantRevoked => "Reconnect",
                CottonSyncRootReadinessStatus.LocalRootUnavailable => "Unavailable",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(readinessStatus),
                    "Sync root readiness status is not supported."),
            };
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
