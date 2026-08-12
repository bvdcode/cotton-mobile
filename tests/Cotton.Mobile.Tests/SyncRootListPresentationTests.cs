using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncRootListPresentationTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncRootListPresentationTests
    {
        [Fact]
        public void EmptyStateIsExplicit()
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([]);

            Assert.Empty(state.Items);
            Assert.Equal("No folders syncing", state.SummaryText);
            Assert.True(state.IsEmptyVisible);
            Assert.False(state.HasItems);
            Assert.False(state.CanRunAny);
        }

        [Fact]
        public void ReadyCloudToDeviceRootHasStableDisplayCopy()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice);

            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([root]);
            CottonSyncRootListItem item = Assert.Single(state.Items);

            Assert.True(state.CanRunAny);
            Assert.Equal(FirstRootId, item.Id);
            Assert.Equal(CottonSyncDirection.CloudToDevice, item.Direction);
            Assert.Equal("Projects", item.Title);
            Assert.Equal("Files / Projects", item.PathText);
            Assert.Equal("Cloud to device · On this device", item.DetailText);
            Assert.Equal("Ready", item.StatusText);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.False(item.CanReconnect);
            Assert.True(item.CanUsePrimaryAction);
            Assert.Equal("Run now", item.PrimaryActionText);
            Assert.False(item.IsPaused);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.CanPauseSync);
            Assert.Equal("Pause", item.PauseSyncActionText);
            Assert.False(item.CanResumeSync);
            Assert.Equal("Resume", item.ResumeSyncActionText);
            Assert.True(item.CanStopSync);
            Assert.Equal("Stop syncing", item.StopSyncActionText);
        }

        [Theory]
        [InlineData(
            CottonSyncDirection.CloudToDevice,
            CottonSyncRootStorageKind.AppPrivateDirectory,
            "Syncing")]
        [InlineData(
            CottonSyncDirection.DeviceToCloud,
            CottonSyncRootStorageKind.UserSelectedDocumentTree,
            "Uploading")]
        [InlineData(
            CottonSyncDirection.Bidirectional,
            CottonSyncRootStorageKind.UserSelectedDocumentTree,
            "Syncing")]
        public void RunningRootUpdatesStatusInPlace(
            CottonSyncDirection direction,
            CottonSyncRootStorageKind storageKind,
            string expectedStatus)
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                direction,
                storageKind,
                "Device folder");
            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);
            List<string?> changedProperties = [];
            item.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);

            item.SetRunning(isRunning: true);

            Assert.True(item.IsRunning);
            Assert.Equal(expectedStatus, item.StatusText);
            Assert.Contains(nameof(item.IsRunning), changedProperties);
            Assert.Contains(nameof(item.StatusText), changedProperties);

            item.SetRunning(isRunning: false);

            Assert.False(item.IsRunning);
            Assert.Equal("Ready", item.StatusText);
        }

        [Fact]
        public void NonRunnableRootCannotEnterRunningState()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Revoked,
                CottonSyncDirection.Bidirectional,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");
            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Throws<InvalidOperationException>(() => item.SetRunning(isRunning: true));
        }

        [Fact]
        public void AttentionStateIsVisibleForRootsNeedingUserAction()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.NeedsUserGrant,
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");

            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([root]);
            CottonSyncRootListItem item = Assert.Single(state.Items);

            Assert.False(state.CanRunAny);
            Assert.Equal("Choose folder", item.StatusText);
            Assert.False(item.IsReady);
            Assert.True(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
            Assert.True(item.CanReconnect);
            Assert.True(item.CanUsePrimaryAction);
            Assert.Equal("Choose local folder", item.PrimaryActionText);
        }

        [Fact]
        public void RevokedDocumentTreeUsesCompactStatusAndExplicitReconnectAction()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Revoked,
                CottonSyncDirection.Bidirectional,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal("Reconnect", item.StatusText);
            Assert.True(item.CanReconnect);
            Assert.True(item.CanUsePrimaryAction);
            Assert.Equal("Reconnect local folder", item.PrimaryActionText);
        }

        [Fact]
        public void RootsAreSortedByPathThenFolderName()
        {
            CottonSyncRootSnapshot second = CreateRoot(
                SecondRootId,
                SecondFolderId,
                "Archive",
                "Files / Z Archive",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice);
            CottonSyncRootSnapshot first = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / A Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud);

            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([second, first]);

            Assert.Equal("2 folders set to sync", state.SummaryText);
            Assert.True(state.CanRunAny);
            Assert.Equal(["Projects", "Archive"], [.. state.Items.Select(item => item.Title)]);
            Assert.Equal(CottonSyncDirection.DeviceToCloud, state.Items[0].Direction);
            Assert.Equal("Upload new files · On this device", state.Items[0].DetailText);
            Assert.False(state.Items[0].CanRunNow);
        }

        [Fact]
        public void PausedRootIsVisibleButNotRunnable()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice);

            CottonSyncRootListDisplayState state =
                CottonSyncRootListDisplayState.Create([root], new HashSet<Guid> { root.Id });
            CottonSyncRootListItem item = Assert.Single(state.Items);

            Assert.False(state.CanRunAny);
            Assert.Equal("Paused", item.StatusText);
            Assert.True(item.IsPaused);
            Assert.False(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
            Assert.False(item.CanPauseSync);
            Assert.True(item.CanResumeSync);
            Assert.True(item.CanStopSync);
        }

        [Fact]
        public void LegacyCloudToDeviceDocumentTreeRootIsUnsupported()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.CloudToDevice,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal("Unsupported", item.StatusText);
            Assert.True(item.IsUnsupportedLocalRoot);
            Assert.False(item.IsReady);
            Assert.True(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
            Assert.True(item.CanPauseSync);
            Assert.True(item.CanStopSync);
        }

        [Fact]
        public void DeviceToCloudUserSelectedDocumentTreeRootIsReadyAndRunnable()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Camera",
                "Files / Camera",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal("Upload new files · Device folder", item.DetailText);
            Assert.Equal("Ready", item.StatusText);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.True(item.CanPauseSync);
            Assert.True(item.CanStopSync);
        }

        [Fact]
        public void BidirectionalUserSelectedDocumentTreeRootIsReadyAndRunnable()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.Bidirectional,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "Device folder");

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal(CottonSyncDirection.Bidirectional, item.Direction);
            Assert.Equal("Bidirectional · Device folder", item.DetailText);
            Assert.Equal("Ready", item.StatusText);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.True(item.CanPauseSync);
            Assert.True(item.CanStopSync);
        }
    }
}
