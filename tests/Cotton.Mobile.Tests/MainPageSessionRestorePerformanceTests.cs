using Xunit;

namespace Cotton.Mobile.Tests
{
    public class MainPageSessionRestorePerformanceTests
    {
        [Fact]
        public void Authenticated_session_initializes_file_browser_before_session_maintenance()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            int showProfile = content.IndexOf("ShowProfile(profile);", StringComparison.Ordinal);
            int initializeFiles = content.IndexOf(
                "await _fileBrowser.InitializeAsync(result.InstanceUri, accountScopeKey);",
                StringComparison.Ordinal);
            int queueMaintenance = content.IndexOf(
                "QueueAuthenticatedSessionMaintenance(result.InstanceUri, \"authenticated session\");",
                StringComparison.Ordinal);

            Assert.True(showProfile >= 0);
            Assert.True(initializeFiles > showProfile);
            Assert.True(queueMaintenance > initializeFiles);

            string firstFileListPath = content[showProfile..initializeFiles];
            Assert.DoesNotContain("RestoreTransferQueueBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("ResumeQueuedBackgroundTransferBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("RegisterCurrentSessionBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
            Assert.DoesNotContain("ScheduleRemotePushTokenRefreshBestEffortAsync", firstFileListPath, StringComparison.Ordinal);
        }

        [Fact]
        public void Cached_offline_session_initializes_cached_root_before_transfer_activity_refresh()
        {
            string content = RepositoryPath.ReadText("src/Cotton.Mobile/ViewModels/MainPageViewModel.cs");

            int showCachedStatus = content.IndexOf(
                "Display.ShowProfileStatus(OfflineCachedSessionStatus);",
                StringComparison.Ordinal);
            int initializeCachedRoot = content.IndexOf(
                "if (!await _fileBrowser.InitializeCachedRootAsync(instanceUri))",
                StringComparison.Ordinal);
            int queueTransferRefresh = content.IndexOf(
                "QueueTransferActivityRefresh(instanceUri, \"cached offline session\");",
                StringComparison.Ordinal);

            Assert.True(showCachedStatus >= 0);
            Assert.True(initializeCachedRoot > showCachedStatus);
            Assert.True(queueTransferRefresh > initializeCachedRoot);

            string firstCachedFilesPath = content[showCachedStatus..initializeCachedRoot];
            Assert.DoesNotContain("RestoreTransferQueueBestEffortAsync", firstCachedFilesPath, StringComparison.Ordinal);
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
