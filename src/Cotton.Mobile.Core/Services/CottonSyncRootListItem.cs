// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Resources.Localization;
using Cotton.Mobile.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Cotton.Mobile.Services
{
    public class CottonSyncRootListItem : ObservableObject
    {
        private readonly string _idleStatusText;
        private CottonSyncProgressSnapshot? _progress;
        private string? _lastSyncStatusText;

        public CottonSyncRootListItem(
            CottonSyncRootSnapshot root,
            bool isPaused = false,
            CottonAutomaticSyncRootStatusSnapshot? automaticStatus = null,
            bool isDividerVisible = false)
        {
            ArgumentNullException.ThrowIfNull(root);
            if (automaticStatus is not null && automaticStatus.RootId != root.Id)
            {
                throw new ArgumentException("Automatic sync status belongs to a different root.", nameof(automaticStatus));
            }

            Id = root.Id;
            Direction = root.Direction;
            Title = root.CloudFolder.FolderName;
            PathText = root.CloudFolder.Path;
            DetailText = $"{CreateDirectionText(root.Direction)} · {root.LocalRoot.DisplayName}";
            IsPaused = isPaused;
            IsUnsupportedLocalRoot = !isPaused && CottonSyncRootRunCapability.HasUnsupportedLocalRoot(root);
            CanRunNow = !isPaused && CottonSyncRootRunCapability.CanRun(root);
            CanReconnect = root.NeedsUserAction;
            CanUsePrimaryAction = CanReconnect || CanRunNow;
            PrimaryActionText = CreatePrimaryActionText(root, CanReconnect, CanRunNow);
            _idleStatusText = CreateStatusText(root, isPaused, IsUnsupportedLocalRoot);
            IsReady = !isPaused && !IsUnsupportedLocalRoot && CanRunNow;
            IsAttentionVisible = !isPaused
                && (IsUnsupportedLocalRoot || root.NeedsUserAction || !root.CanRunSync || !CanRunNow);
            CanPauseSync = !isPaused;
            CanResumeSync = isPaused;
            CanStopSync = true;
            IsDividerVisible = isDividerVisible;
            SetAutomaticStatus(automaticStatus);
        }

        public Guid Id { get; }

        public CottonSyncDirection Direction { get; }

        public string Title { get; }

        public string PathText { get; }

        public string DetailText { get; }

        public string StatusText => _progress is null
            ? _lastSyncStatusText ?? _idleStatusText
            : CreateProgressStatusText(_progress);

        public bool IsRunning => _progress is not null;

        public bool IsProgressDeterminate =>
            _progress?.Stage == CottonSyncProgressStage.ApplyingChanges
            && _progress.TotalItemCount > 0;

        public double ProgressValue
        {
            get
            {
                CottonSyncProgressSnapshot? progress = _progress;
                if (progress?.TotalItemCount is not int totalItemCount || totalItemCount <= 0)
                {
                    return 0;
                }

                return (double)progress.CompletedItemCount / totalItemCount;
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

        public string PauseSyncActionText { get; } = CottonSyncRootManagementText.PauseAction;

        public bool CanResumeSync { get; }

        public string ResumeSyncActionText { get; } = CottonSyncRootManagementText.ResumeAction;

        public bool CanStopSync { get; }

        public string StopSyncActionText { get; } = CottonSyncRootManagementText.StopAction;

        public bool IsDividerVisible { get; }

        public void ApplyProgress(CottonSyncProgressSnapshot progress)
        {
            ArgumentNullException.ThrowIfNull(progress);
            if (progress.RootId != Id)
            {
                throw new ArgumentException("Sync progress belongs to a different root.", nameof(progress));
            }

            if (!CanRunNow)
            {
                throw new InvalidOperationException("Only a runnable sync root can report progress.");
            }

            _progress = progress;
            NotifyProgressChanged();
        }

        public void CompleteProgress()
        {
            if (_progress is null)
            {
                return;
            }

            _progress = null;
            NotifyProgressChanged();
        }

        public void SetAutomaticStatus(CottonAutomaticSyncRootStatusSnapshot? status)
        {
            if (status is not null && status.RootId != Id)
            {
                throw new ArgumentException("Automatic sync status belongs to a different root.", nameof(status));
            }

            string? statusText = IsReady && status is not null
                ? CottonAutomaticSyncStatusText.Create([status])
                : null;
            if (string.Equals(_lastSyncStatusText, statusText, StringComparison.Ordinal))
            {
                return;
            }

            _lastSyncStatusText = statusText;
            if (!IsRunning)
            {
                OnPropertyChanged(nameof(StatusText));
            }
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
            bool isUnsupportedLocalRoot)
        {
            if (isPaused)
            {
                return CottonSyncRootManagementText.PausedStatusText;
            }

            if (isUnsupportedLocalRoot)
            {
                return CoreResources.UnsupportedStatus;
            }

            CottonSyncRootReadinessStatus readinessStatus = root.ReadinessStatus;
            return readinessStatus switch
            {
                CottonSyncRootReadinessStatus.Ready => CoreResources.ReadyStatus,
                CottonSyncRootReadinessStatus.NeedsUserGrant when root.LocalRoot.UsesMediaStore =>
                    CoreResources.ChooseMediaStatus,
                CottonSyncRootReadinessStatus.NeedsUserGrant => CoreResources.ChooseFolderStatus,
                CottonSyncRootReadinessStatus.GrantRevoked when root.LocalRoot.UsesMediaStore =>
                    CoreResources.ReconnectMediaStatus,
                CottonSyncRootReadinessStatus.GrantRevoked => CoreResources.ReconnectStatus,
                CottonSyncRootReadinessStatus.LocalRootUnavailable => CoreResources.UnavailableStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(root),
                    readinessStatus,
                    "Sync root readiness status is not supported."),
            };
        }

        private static string CreateProgressStatusText(CottonSyncProgressSnapshot progress)
        {
            return progress.Stage switch
            {
                CottonSyncProgressStage.ScanningDevice => CoreResources.ScanningDeviceStatus,
                CottonSyncProgressStage.CheckingCloud => CoreResources.CheckingCloudStatus,
                CottonSyncProgressStage.ApplyingChanges when progress.TotalItemCount > 0 => CoreResources.Format(
                    CoreResources.ApplyingChangesFormat,
                    progress.CompletedItemCount,
                    progress.TotalItemCount.Value),
                CottonSyncProgressStage.ApplyingChanges => CoreResources.FinishingSyncStatus,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(progress),
                    progress.Stage,
                    "Sync progress stage is not supported."),
            };
        }

        private static string CreateDirectionText(CottonSyncDirection direction)
        {
            EnsureSupportedDirection(direction);
            return CoreResources.UploadNewFilesAction;
        }

        private static void EnsureSupportedDirection(CottonSyncDirection direction)
        {
            if (direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new ArgumentOutOfRangeException(nameof(direction), "Sync direction is not supported.");
            }
        }

        private void NotifyProgressChanged()
        {
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsProgressDeterminate));
            OnPropertyChanged(nameof(ProgressValue));
        }
    }
}
