// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.ViewModels;

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListItem : ViewModelBase
    {
        private readonly string _idleStatusText;
        private readonly string _runningStatusText;
        private bool _isRunning;

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
            _idleStatusText = CreateStatusText(root, isPaused, IsUnsupportedLocalRoot, CanRunNow);
            _runningStatusText = CreateRunningStatusText(root.Direction);
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

        public string StatusText => IsRunning ? _runningStatusText : _idleStatusText;

        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(StatusText));
                }
            }
        }

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

        public void SetRunning(bool isRunning)
        {
            if (isRunning && !CanRunNow)
            {
                throw new InvalidOperationException("Only a runnable sync root can enter the running state.");
            }

            IsRunning = isRunning;
        }

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
                return CoreResources.RunNowAction;
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
                return CoreResources.UnsupportedStatus;
            }

            if (!canRunNow && root.Direction == CottonSyncDirection.Bidirectional && root.CanRunSync)
            {
                return CoreResources.UnavailableStatus;
            }

            CottonSyncRootReadinessStatus readinessStatus = root.ReadinessStatus;
            return readinessStatus switch
            {
                CottonSyncRootReadinessStatus.Ready => CoreResources.ReadyStatus,
                CottonSyncRootReadinessStatus.NeedsUserGrant => CoreResources.ChooseFolderStatus,
                CottonSyncRootReadinessStatus.GrantRevoked => CoreResources.ReconnectStatus,
                CottonSyncRootReadinessStatus.LocalRootUnavailable => CoreResources.UnavailableStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(readinessStatus),
                    "Sync root readiness status is not supported."),
            };
        }

        private static string CreateRunningStatusText(CottonSyncDirection direction)
        {
            return direction switch
            {
                CottonSyncDirection.CloudToDevice => CoreResources.SyncingStatus,
                CottonSyncDirection.DeviceToCloud => CoreResources.UploadingStatus,
                CottonSyncDirection.Bidirectional => CoreResources.SyncingStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    "Sync direction is not supported."),
            };
        }

        private static string CreateDirectionText(CottonSyncDirection direction)
        {
            return direction switch
            {
                CottonSyncDirection.CloudToDevice => CoreResources.CloudToDeviceDirection,
                CottonSyncDirection.DeviceToCloud => CoreResources.UploadNewFilesAction,
                CottonSyncDirection.Bidirectional => CoreResources.BidirectionalDirection,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported."),
            };
        }
    }
}
