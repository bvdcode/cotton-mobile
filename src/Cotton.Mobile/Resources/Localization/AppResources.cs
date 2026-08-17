// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using System.Resources;
using System.Text;

namespace Cotton.Mobile.Resources.Localization
{
    public static class AppResources
    {
        private static readonly ResourceManager ResourceManagerInstance = new(typeof(AppResources));

        public static string AppTitle => GetString(nameof(AppTitle));
        public static string FilesTitle => GetString(nameof(FilesTitle));
        public static string ChooseCloudFolderPageTitle => GetString(nameof(ChooseCloudFolderPageTitle));
        public static string CloudFolderAppBarTitle => GetString(nameof(CloudFolderAppBarTitle));
        public static string UseCurrentCloudFolderDescription => GetString(nameof(UseCurrentCloudFolderDescription));
        public static string GoToParentFolderDescription => GetString(nameof(GoToParentFolderDescription));
        public static string NoSubfoldersTitle => GetString(nameof(NoSubfoldersTitle));
        public static string NoSubfoldersBody => GetString(nameof(NoSubfoldersBody));
        public static string ProfileTitle => GetString(nameof(ProfileTitle));
        public static string AccountTitle => GetString(nameof(AccountTitle));
        public static string AccountSupportingText => GetString(nameof(AccountSupportingText));
        public static string PrivacyPolicyText => GetString(nameof(PrivacyPolicyText));
        public static string PrivacyPolicySupportingText => GetString(nameof(PrivacyPolicySupportingText));
        public static string OpenPrivacyPolicyDescription => GetString(nameof(OpenPrivacyPolicyDescription));
        public static string SignOutText => GetString(nameof(SignOutText));
        public static string SignOutSupportingText => GetString(nameof(SignOutSupportingText));
        public static string WaitingForBrowserApproval => GetString(nameof(WaitingForBrowserApproval));
        public static string CancelBrowserApprovalDescription => GetString(nameof(CancelBrowserApprovalDescription));
        public static string BackDescription => GetString(nameof(BackDescription));
        public static string AddSyncFolderDescription => GetString(nameof(AddSyncFolderDescription));
        public static string RunAllSyncFoldersDescription => GetString(nameof(RunAllSyncFoldersDescription));
        public static string RefreshSyncFoldersDescription => GetString(nameof(RefreshSyncFoldersDescription));
        public static string SyncTitle => GetString(nameof(SyncTitle));
        public static string NoSyncFoldersTitle => GetString(nameof(NoSyncFoldersTitle));
        public static string NoSyncFoldersBody => GetString(nameof(NoSyncFoldersBody));
        public static string ConnectText => GetString(nameof(ConnectText));
        public static string ChangeServerText => GetString(nameof(ChangeServerText));
        public static string CottonCloudAddressHint => GetString(nameof(CottonCloudAddressHint));
        public static string OpenSyncDescription => GetString(nameof(OpenSyncDescription));
        public static string OpenProfileDescription => GetString(nameof(OpenProfileDescription));
        public static string PrimaryActionDescription => GetString(nameof(PrimaryActionDescription));
        public static string SecondaryActionDescription => GetString(nameof(SecondaryActionDescription));
        public static string TertiaryActionDescription => GetString(nameof(TertiaryActionDescription));
        public static string PrivacyText => GetString(nameof(PrivacyText));
        public static string RepositoryText => GetString(nameof(RepositoryText));
        public static string RepositoryTitle => GetString(nameof(RepositoryTitle));
        public static string RepositoryOpenFailed => GetString(nameof(RepositoryOpenFailed));
        public static string CloudFoldersLoadFailed => GetString(nameof(CloudFoldersLoadFailed));
        public static string CloudFolderOpenFailed => GetString(nameof(CloudFolderOpenFailed));
        public static string MoreSyncActionsDescription => GetString(nameof(MoreSyncActionsDescription));
        public static string SyncSettingsUpdateFailed => GetString(nameof(SyncSettingsUpdateFailed));
        public static string SyncFoldersInspectFailed => GetString(nameof(SyncFoldersInspectFailed));
        public static string SyncFolderAccountUnavailable => GetString(nameof(SyncFolderAccountUnavailable));
        public static string SyncFolderAddOffline => GetString(nameof(SyncFolderAddOffline));
        public static string SyncFolderAddFailed => GetString(nameof(SyncFolderAddFailed));
        public static string SyncInitialRunFailed => GetString(nameof(SyncInitialRunFailed));
        public static string LocalFolderReconnectFailed => GetString(nameof(LocalFolderReconnectFailed));
        public static string LocalFolderAccessAvailable => GetString(nameof(LocalFolderAccessAvailable));
        public static string SyncRunInstanceUnavailable => GetString(nameof(SyncRunInstanceUnavailable));
        public static string SyncFolderMissing => GetString(nameof(SyncFolderMissing));
        public static string SyncFolderNotReady => GetString(nameof(SyncFolderNotReady));
        public static string InvalidServerUrl => GetString(nameof(InvalidServerUrl));
        public static string CheckingServer => GetString(nameof(CheckingServer));
        public static string CheckingSession => GetString(nameof(CheckingSession));
        public static string ServerNotFound => GetString(nameof(ServerNotFound));
        public static string InsecureConnectionTitle => GetString(nameof(InsecureConnectionTitle));
        public static string ContinueText => GetString(nameof(ContinueText));
        public static string SigningOut => GetString(nameof(SigningOut));
        public static string UnexpectedError => GetString(nameof(UnexpectedError));
        public static string SignOutQuestion => GetString(nameof(SignOutQuestion));
        public static string SignOutConfirmation => GetString(nameof(SignOutConfirmation));
        public static string CancelText => GetString(nameof(CancelText));
        public static string PrivacyPolicyTitle => GetString(nameof(PrivacyPolicyTitle));
        public static string PrivacyPolicyOpenFailed => GetString(nameof(PrivacyPolicyOpenFailed));
        public static string OkText => GetString(nameof(OkText));
        public static string SessionRestoreFailed => GetString(nameof(SessionRestoreFailed));
        public static string SessionRestoreOffline => GetString(nameof(SessionRestoreOffline));
        public static string SignedOutStatus => GetString(nameof(SignedOutStatus));
        public static string SignOutFailed => GetString(nameof(SignOutFailed));
        public static string SignOutCompletionFailed => GetString(nameof(SignOutCompletionFailed));
        public static string SessionVerificationFailed => GetString(nameof(SessionVerificationFailed));
        public static string SessionOffline => GetString(nameof(SessionOffline));
        public static string AuthorizationCancelled => GetString(nameof(AuthorizationCancelled));
        public static string AuthorizationDenied => GetString(nameof(AuthorizationDenied));
        public static string AuthorizationExpired => GetString(nameof(AuthorizationExpired));
        public static string AuthorizationNotFound => GetString(nameof(AuthorizationNotFound));
        public static string BrowserUnavailable => GetString(nameof(BrowserUnavailable));
        public static string AuthorizationTimedOut => GetString(nameof(AuthorizationTimedOut));
        public static string AuthorizationFailed => GetString(nameof(AuthorizationFailed));
        public static string SessionExpired => GetString(nameof(SessionExpired));
        public static string AuthorizationPending => GetString(nameof(AuthorizationPending));
        public static string AuthorizationFailure => GetString(nameof(AuthorizationFailure));
        public static string DefaultUserName => GetString(nameof(DefaultUserName));
        public static string UnknownText => GetString(nameof(UnknownText));
        public static string DebugInstallChannel => GetString(nameof(DebugInstallChannel));
        public static string ReleaseInstallChannel => GetString(nameof(ReleaseInstallChannel));
        public static string CustomInstallChannel => GetString(nameof(CustomInstallChannel));
        public static string SelectedFolder => GetString(nameof(SelectedFolder));
        public static string NotificationChannelName => GetString(nameof(NotificationChannelName));
        public static string NotificationChannelDescription => GetString(nameof(NotificationChannelDescription));
        public static string SecurityNotificationChannelName => GetString(nameof(SecurityNotificationChannelName));
        public static string SecurityNotificationChannelDescription => GetString(nameof(SecurityNotificationChannelDescription));

        public static string CreateInsecureConnectionMessage(Uri instanceUri)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            CompositeFormat format = CompositeFormat.Parse(GetString("InsecureConnectionFormat"));
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                instanceUri.AbsoluteUri);
        }

        public static string CreateNotificationSummary(string latestTitle, int additionalCount)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(latestTitle);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(additionalCount);

            string format = additionalCount == 1
                ? GetString("NotificationSummarySingleFormat")
                : GetString("NotificationSummaryFormat");
            return string.Format(CultureInfo.CurrentCulture, format, latestTitle, additionalCount);
        }

        public static string CreateVersionText(string version, string build)
        {
            string format = string.IsNullOrWhiteSpace(build)
                ? GetString("VersionFormat")
                : GetString("VersionWithBuildFormat");
            return string.IsNullOrWhiteSpace(build)
                ? string.Format(CultureInfo.CurrentCulture, format, version)
                : string.Format(CultureInfo.CurrentCulture, format, version, build);
        }

        private static string GetString(string name)
        {
            return ResourceManagerInstance.GetString(name, CultureInfo.CurrentUICulture)
                ?? throw new InvalidOperationException($"App resource '{name}' is missing.");
        }
    }
}
