// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;

namespace Cotton.Mobile.Resources.Localization
{
    public static class CoreResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(CoreResources));

        public static string AuthorizationInstruction => GetString(nameof(AuthorizationInstruction));
        public static string CustomServerUrl => GetString(nameof(CustomServerUrl));
        public static string CancellingAuthorization => GetString(nameof(CancellingAuthorization));
        public static string DefaultFolderName => GetString(nameof(DefaultFolderName));
        public static string DefaultFolderNameLower => GetString(nameof(DefaultFolderNameLower));
        public static string DefaultFolderReference => GetString(nameof(DefaultFolderReference));
        public static string SyncingFolders => GetString(nameof(SyncingFolders));
        public static string AccountSessionRequired => GetString(nameof(AccountSessionRequired));
        public static string SyncOffline => GetString(nameof(SyncOffline));
        public static string SyncCancelled => GetString(nameof(SyncCancelled));
        public static string SyncFailed => GetString(nameof(SyncFailed));
        public static string SyncingFolderFormat => GetString(nameof(SyncingFolderFormat));
        public static string NoSyncFolders => GetString(nameof(NoSyncFolders));
        public static string SyncCurrent => GetString(nameof(SyncCurrent));
        public static string SyncCompletedFormat => GetString(nameof(SyncCompletedFormat));
        public static string UploadNewFilesAction => GetString(nameof(UploadNewFilesAction));
        public static string UploadingFolderFormat => GetString(nameof(UploadingFolderFormat));
        public static string StopSyncingAction => GetString(nameof(StopSyncingAction));
        public static string PauseAction => GetString(nameof(PauseAction));
        public static string ResumeAction => GetString(nameof(ResumeAction));
        public static string CancelAction => GetString(nameof(CancelAction));
        public static string StopSyncingMessage => GetString(nameof(StopSyncingMessage));
        public static string PausedStatus => GetString(nameof(PausedStatus));
        public static string RootPausedStatus => GetString(nameof(RootPausedStatus));
        public static string RootMissingStatus => GetString(nameof(RootMissingStatus));
        public static string PauseFailedStatus => GetString(nameof(PauseFailedStatus));
        public static string ResumeFailedStatus => GetString(nameof(ResumeFailedStatus));
        public static string StopFailedStatus => GetString(nameof(StopFailedStatus));
        public static string StopSyncingTitleFormat => GetString(nameof(StopSyncingTitleFormat));
        public static string StoppedSyncingFormat => GetString(nameof(StoppedSyncingFormat));
        public static string PausedSyncingFormat => GetString(nameof(PausedSyncingFormat));
        public static string ResumedSyncingFormat => GetString(nameof(ResumedSyncingFormat));
        public static string NoFoldersSyncing => GetString(nameof(NoFoldersSyncing));
        public static string OneFolderSyncing => GetString(nameof(OneFolderSyncing));
        public static string FoldersSyncingFormat => GetString(nameof(FoldersSyncingFormat));
        public static string RunNowAction => GetString(nameof(RunNowAction));
        public static string UnsupportedStatus => GetString(nameof(UnsupportedStatus));
        public static string UnavailableStatus => GetString(nameof(UnavailableStatus));
        public static string ReadyStatus => GetString(nameof(ReadyStatus));
        public static string ChooseFolderStatus => GetString(nameof(ChooseFolderStatus));
        public static string ReconnectStatus => GetString(nameof(ReconnectStatus));
        public static string SyncingStatus => GetString(nameof(SyncingStatus));
        public static string UploadingStatus => GetString(nameof(UploadingStatus));
        public static string SyncRootReady => GetString(nameof(SyncRootReady));
        public static string ChooseLocalFolder => GetString(nameof(ChooseLocalFolder));
        public static string ReconnectLocalFolder => GetString(nameof(ReconnectLocalFolder));
        public static string LocalFolderUnavailable => GetString(nameof(LocalFolderUnavailable));
        public static string LocalSyncTargetUnsupported => GetString(nameof(LocalSyncTargetUnsupported));
        public static string LocalSyncSourceUnsupported => GetString(nameof(LocalSyncSourceUnsupported));
        public static string OnDeviceStatus => GetString(nameof(OnDeviceStatus));
        public static string OfflineMissingStatus => GetString(nameof(OfflineMissingStatus));
        public static string OfflineStaleStatus => GetString(nameof(OfflineStaleStatus));
        public static string AvailableOfflineDetails => GetString(nameof(AvailableOfflineDetails));
        public static string MissingOfflineDetails => GetString(nameof(MissingOfflineDetails));
        public static string StaleOfflineDetails => GetString(nameof(StaleOfflineDetails));
        public static string OpenAction => GetString(nameof(OpenAction));
        public static string MoreAction => GetString(nameof(MoreAction));
        public static string OpenWithSystemAppAction => GetString(nameof(OpenWithSystemAppAction));
        public static string OpenUnavailable => GetString(nameof(OpenUnavailable));
        public static string PdfOpenUnavailable => GetString(nameof(PdfOpenUnavailable));
        public static string DocumentOpenUnavailable => GetString(nameof(DocumentOpenUnavailable));
        public static string AudioOpenUnavailable => GetString(nameof(AudioOpenUnavailable));
        public static string VideoOpenUnavailable => GetString(nameof(VideoOpenUnavailable));
        public static string ArchiveOpenUnavailable => GetString(nameof(ArchiveOpenUnavailable));
        public static string SvgOpenUnavailable => GetString(nameof(SvgOpenUnavailable));
        public static string UnknownOpenUnavailable => GetString(nameof(UnknownOpenUnavailable));
        public static string UnnamedItem => GetString(nameof(UnnamedItem));
        public static string FolderKind => GetString(nameof(FolderKind));
        public static string FileKind => GetString(nameof(FileKind));
        public static string UploadedFileName => GetString(nameof(UploadedFileName));
        public static string DownloadedLabel => GetString(nameof(DownloadedLabel));
        public static string RefreshedLabel => GetString(nameof(RefreshedLabel));
        public static string RenamedLabel => GetString(nameof(RenamedLabel));
        public static string RemovedLabel => GetString(nameof(RemovedLabel));
        public static string BlockedLabel => GetString(nameof(BlockedLabel));
        public static string UploadedLabel => GetString(nameof(UploadedLabel));
        public static string RefreshedLocallyLabel => GetString(nameof(RefreshedLocallyLabel));
        public static string RenamedLocallyLabel => GetString(nameof(RenamedLocallyLabel));
        public static string RemovedLocallyLabel => GetString(nameof(RemovedLocallyLabel));
        public static string UpdatedInCloudLabel => GetString(nameof(UpdatedInCloudLabel));
        public static string FolderCreatedSingular => GetString(nameof(FolderCreatedSingular));
        public static string FolderCreatedPlural => GetString(nameof(FolderCreatedPlural));
        public static string RemoteFileRemovedSingular => GetString(nameof(RemoteFileRemovedSingular));
        public static string RemoteFileRemovedPlural => GetString(nameof(RemoteFileRemovedPlural));
        public static string RecordCleanedSingular => GetString(nameof(RecordCleanedSingular));
        public static string RecordCleanedPlural => GetString(nameof(RecordCleanedPlural));
        public static string ConflictReviewSingular => GetString(nameof(ConflictReviewSingular));
        public static string ConflictReviewPlural => GetString(nameof(ConflictReviewPlural));
        public static string LocalRemovalReviewSingular => GetString(nameof(LocalRemovalReviewSingular));
        public static string LocalRemovalReviewPlural => GetString(nameof(LocalRemovalReviewPlural));
        public static string CloudRemovalReviewSingular => GetString(nameof(CloudRemovalReviewSingular));
        public static string CloudRemovalReviewPlural => GetString(nameof(CloudRemovalReviewPlural));
        public static string RootSkippedSingular => GetString(nameof(RootSkippedSingular));
        public static string RootSkippedPlural => GetString(nameof(RootSkippedPlural));
        public static string UploadConfirmedSingular => GetString(nameof(UploadConfirmedSingular));
        public static string UploadConfirmedPlural => GetString(nameof(UploadConfirmedPlural));
        public static string OriginalRemovedSingular => GetString(nameof(OriginalRemovedSingular));
        public static string OriginalRemovedPlural => GetString(nameof(OriginalRemovedPlural));
        public static string FilesName => GetString(nameof(FilesName));
        public static string LocalFolderName => GetString(nameof(LocalFolderName));
        public static string SyncRootCompleted => GetString(nameof(SyncRootCompleted));
        public static string UnsyncableName => GetString(nameof(UnsyncableName));
        public static string UnnamedName => GetString(nameof(UnnamedName));
        public static string ImageKind => GetString(nameof(ImageKind));
        public static string PdfKind => GetString(nameof(PdfKind));
        public static string DocumentKind => GetString(nameof(DocumentKind));
        public static string VideoKind => GetString(nameof(VideoKind));
        public static string AudioKind => GetString(nameof(AudioKind));
        public static string SvgKind => GetString(nameof(SvgKind));
        public static string TextKind => GetString(nameof(TextKind));
        public static string FileBadge => GetString(nameof(FileBadge));
        public static string ImageBadge => GetString(nameof(ImageBadge));
        public static string DocumentBadge => GetString(nameof(DocumentBadge));
        public static string VideoBadge => GetString(nameof(VideoBadge));
        public static string AudioBadge => GetString(nameof(AudioBadge));
        public static string TextBadge => GetString(nameof(TextBadge));

        public static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.CurrentCulture, format, arguments);
        }

        private static string GetString(string name)
        {
            return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException($"Core resource '{name}' is missing.");
        }
    }
}
