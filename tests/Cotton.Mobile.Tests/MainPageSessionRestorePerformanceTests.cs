using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MainPageSessionRestorePerformanceTests
    {
        [Fact]
        public void Session_restore_does_not_expose_internal_operation_copy()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            Assert.Contains("ShowLoading(string.Empty);", content, StringComparison.Ordinal);
            Assert.DoesNotContain("Restoring session...", content, StringComparison.Ordinal);

            int showCachedSession = content.IndexOf(
                "didShowCachedSession = await TryShowCachedSessionDuringRestoreAsync();",
                StringComparison.Ordinal);
            int restoreSession = content.IndexOf(
                "CottonSessionResult result = await _sessionService.RestoreAsync();",
                StringComparison.Ordinal);

            Assert.True(showCachedSession >= 0);
            Assert.True(restoreSession > showCachedSession);
        }

        [Fact]
        public void Authenticated_session_initializes_file_browser_before_session_maintenance()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");
            int applySessionStart = content.IndexOf(
                "private async Task ApplySessionResultAsync(",
                StringComparison.Ordinal);
            int applySessionEnd = content.IndexOf(
                "private string ResolveSessionStatusMessage",
                applySessionStart,
                StringComparison.Ordinal);
            string applySession = content[applySessionStart..applySessionEnd];

            int showProfile = applySession.IndexOf("ShowProfile(profile);", StringComparison.Ordinal);
            int initializeFiles = applySession.IndexOf(
                "await _fileBrowser.InitializeAsync(result.InstanceUri, accountScopeKey);",
                StringComparison.Ordinal);
            int queueMaintenance = applySession.IndexOf(
                "QueueAuthenticatedSessionMaintenance(result.InstanceUri, \"authenticated session\");",
                StringComparison.Ordinal);

            Assert.True(showProfile >= 0);
            Assert.True(initializeFiles > showProfile);
            Assert.True(queueMaintenance > initializeFiles);

            string firstFileListPath = applySession[showProfile..initializeFiles];
            Assert.DoesNotContain("RestoreTransferQueueBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("ResumeQueuedBackgroundTransferBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("RegisterCurrentSessionBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("ScheduleRemotePushTokenRefreshBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
        }

        [Fact]
        public void Remembered_session_preserves_cached_root_during_authenticated_refresh()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            int refreshProfile = content.IndexOf(
                "Display.RefreshProfile(profile);",
                StringComparison.Ordinal);
            int refreshCachedRoot = content.IndexOf(
                "await _fileBrowser.InitializeAuthenticatedSessionFromCachedRootAsync(",
                StringComparison.Ordinal);
            int queueTransferRefresh = content.IndexOf(
                "QueueAuthenticatedSessionMaintenance(result.InstanceUri, \"authenticated session\");",
                StringComparison.Ordinal);

            Assert.True(refreshProfile >= 0);
            Assert.True(refreshCachedRoot > refreshProfile);
            Assert.True(queueTransferRefresh > refreshCachedRoot);
        }

        [Fact]
        public void Cached_session_requires_remembered_credentials_and_loads_before_network_restore()
        {
            string viewModel = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");
            string sessionService = RepositoryPath.ReadText("src/Cotton.Mobile/Services/CottonSessionService.cs");
            string fileBrowser = RepositoryPath.ReadText(
                "src/Cotton.Mobile/ViewModels/MainPageFileBrowserController.cs");

            Assert.Contains("_sessionService.GetRememberedSessionInstanceAsync()", viewModel, StringComparison.Ordinal);
            Assert.Contains("TokenPairDto? tokens = await _tokenStore.GetAsync", sessionService, StringComparison.Ordinal);
            Assert.Contains("return tokens is null ? null : instanceUri;", sessionService, StringComparison.Ordinal);
            Assert.Contains(
                "InitializeCachedRootAsync(instanceUri, showOfflineNotice)",
                viewModel,
                StringComparison.Ordinal);
            int initializeCachedRoot = viewModel.IndexOf(
                "InitializeCachedRootAsync(instanceUri, showOfflineNotice)",
                StringComparison.Ordinal);
            int revealCachedProfile = viewModel.IndexOf(
                "Display.ShowProfileWithCachedFiles(profile);",
                StringComparison.Ordinal);
            Assert.True(revealCachedProfile > initializeCachedRoot);
            Assert.Contains("isRefresh: true, isSilentRefresh: true", fileBrowser, StringComparison.Ordinal);
            Assert.Contains("blocksInteraction: !isSilentRefresh", fileBrowser, StringComparison.Ordinal);
            Assert.Contains("_isFolderNavigationInProgress", fileBrowser, StringComparison.Ordinal);
            Assert.Contains("|| !IsDisplayingRootFolder()", fileBrowser, StringComparison.Ordinal);
            Assert.Contains(
                "(_isFileLoadInProgress && _isFileLoadInteractionBlocking)",
                fileBrowser,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Deferred_session_maintenance_checks_that_session_is_still_current()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            Assert.Contains("RunAuthenticatedSessionMaintenanceBestEffortAsync(instanceUri, reason)", content, StringComparison.Ordinal);
            Assert.Contains("IsCurrentVisibleSessionOnMainThreadAsync(instanceUri)", content, StringComparison.Ordinal);
            Assert.Contains("Display.IsProfileVisible && Uri.Equals(ResolveInstanceUri(), instanceUri)", content, StringComparison.Ordinal);
            Assert.Contains(
                "_remotePushRegistrationService.RegisterCurrentSessionBestEffortAsync(instanceUri)",
                content,
                StringComparison.Ordinal);
        }
    }
}
