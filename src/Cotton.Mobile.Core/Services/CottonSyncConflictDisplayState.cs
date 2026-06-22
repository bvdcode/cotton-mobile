// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncConflictDisplayState
    {
        private CottonSyncConflictDisplayState(
            bool isVisible,
            bool isBlocking,
            string title,
            string detailText,
            IReadOnlyList<CottonSyncConflictActionSnapshot> actions)
        {
            ArgumentNullException.ThrowIfNull(actions);

            IsVisible = isVisible;
            IsBlocking = isBlocking;
            Title = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
            DetailText = string.IsNullOrWhiteSpace(detailText) ? string.Empty : detailText.Trim();
            Actions = actions;
        }

        public bool IsVisible { get; }

        public bool IsBlocking { get; }

        public string Title { get; }

        public string DetailText { get; }

        public IReadOnlyList<CottonSyncConflictActionSnapshot> Actions { get; }

        public bool HasPrimaryAction => Actions.Any(action => action.IsPrimary);

        public static CottonSyncConflictDisplayState Hidden { get; } = new(
            isVisible: false,
            isBlocking: false,
            title: string.Empty,
            detailText: string.Empty,
            Array.Empty<CottonSyncConflictActionSnapshot>());

        public static CottonSyncConflictDisplayState Create(
            CottonSyncJournalItemSnapshot item,
            CottonSyncConflictDetectionSnapshot detection)
        {
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(detection);

            if (detection.CanExecuteServerMutation
                || detection.CanCompleteWithoutServerMutation
                || detection.Status is CottonSyncConflictDetectionStatus.TerminalJournalItem
                    or CottonSyncConflictDetectionStatus.WrongSyncRoot)
            {
                return Hidden;
            }

            return detection.Status switch
            {
                CottonSyncConflictDetectionStatus.NeedsFreshServerRevision => CreateRefreshState(item),
                CottonSyncConflictDetectionStatus.ServerRevisionChanged => CreateChangedState(item),
                CottonSyncConflictDetectionStatus.RemoteTargetTypeChanged => CreateChangedState(item),
                CottonSyncConflictDetectionStatus.RemoteTargetMismatch => CreateChangedState(item),
                CottonSyncConflictDetectionStatus.RemoteTargetMissing => CreateMissingState(item),
                CottonSyncConflictDetectionStatus.FolderRevisionUnsupported => CreateFolderUnsupportedState(item),
                CottonSyncConflictDetectionStatus.RootNotReady => CreateRootNotReadyState(item),
                _ => Hidden,
            };
        }

        private static CottonSyncConflictDisplayState CreateRefreshState(CottonSyncJournalItemSnapshot item)
        {
            return Visible(
                isBlocking: true,
                "Refresh needed",
                $"Refresh {item.DisplayName} before syncing this change.",
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.Refresh,
                    "Refresh",
                    isPrimary: true,
                    isDestructive: false));
        }

        private static CottonSyncConflictDisplayState CreateChangedState(CottonSyncJournalItemSnapshot item)
        {
            return Visible(
                isBlocking: true,
                "File changed elsewhere",
                $"{item.DisplayName} changed before this sync ran.",
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.UseCloudVersion,
                    "Use cloud",
                    isPrimary: true,
                    isDestructive: false),
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.KeepLocalChange,
                    "Keep local",
                    isPrimary: false,
                    isDestructive: false));
        }

        private static CottonSyncConflictDisplayState CreateMissingState(CottonSyncJournalItemSnapshot item)
        {
            return Visible(
                isBlocking: true,
                "File missing in cloud",
                $"{item.DisplayName} is no longer available in the cloud.",
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.SkipLocalChange,
                    "Skip change",
                    isPrimary: true,
                    isDestructive: true),
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.KeepLocalChange,
                    "Keep local",
                    isPrimary: false,
                    isDestructive: false));
        }

        private static CottonSyncConflictDisplayState CreateFolderUnsupportedState(CottonSyncJournalItemSnapshot item)
        {
            return Visible(
                isBlocking: true,
                "Folder sync blocked",
                $"{item.DisplayName} cannot sync until folder revisions are available.",
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.Dismiss,
                    "Dismiss",
                    isPrimary: true,
                    isDestructive: false));
        }

        private static CottonSyncConflictDisplayState CreateRootNotReadyState(CottonSyncJournalItemSnapshot item)
        {
            return Visible(
                isBlocking: true,
                "Sync paused",
                $"Reconnect the local folder before syncing {item.DisplayName}.",
                new CottonSyncConflictActionSnapshot(
                    CottonSyncConflictResolutionAction.ReconnectLocalRoot,
                    "Reconnect",
                    isPrimary: true,
                    isDestructive: false));
        }

        private static CottonSyncConflictDisplayState Visible(
            bool isBlocking,
            string title,
            string detailText,
            params CottonSyncConflictActionSnapshot[] actions)
        {
            return new CottonSyncConflictDisplayState(
                isVisible: true,
                isBlocking,
                title,
                detailText,
                actions);
        }
    }
}
