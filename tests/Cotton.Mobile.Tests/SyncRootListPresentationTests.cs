using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.SyncRootListPresentationTestData;

namespace Cotton.Mobile.Tests
{
    public class SyncRootListPresentationTests
    {
        [Fact]
        public void Empty_state_is_explicit()
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([]);

            Assert.Empty(state.Items);
            Assert.Equal("No folders syncing", state.SummaryText);
            Assert.True(state.IsEmptyVisible);
            Assert.False(state.HasItems);
            Assert.False(state.CanRunAny);
        }

        [Fact]
        public void Ready_cloud_to_device_root_has_stable_display_copy()
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
        [InlineData(CottonSyncDirection.CloudToDevice, "Syncing")]
        [InlineData(CottonSyncDirection.DeviceToCloud, "Uploading")]
        [InlineData(CottonSyncDirection.Bidirectional, "Syncing")]
        public void Running_root_updates_status_in_place(
            CottonSyncDirection direction,
            string expectedStatus)
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Projects",
                "Files / Projects",
                CottonSyncRootPermissionStatus.Available,
                direction,
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
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
        public void Non_runnable_root_cannot_enter_running_state()
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
        public void Attention_state_is_visible_for_roots_needing_user_action()
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
        public void Revoked_document_tree_uses_compact_status_and_explicit_reconnect_action()
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
        public void Roots_are_sorted_by_path_then_folder_name()
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
            Assert.Equal(["Projects", "Archive"], state.Items.Select(item => item.Title).ToArray());
            Assert.Equal(CottonSyncDirection.DeviceToCloud, state.Items[0].Direction);
            Assert.Equal("Upload new files · On this device", state.Items[0].DetailText);
            Assert.False(state.Items[0].CanRunNow);
        }

        [Fact]
        public void Paused_root_is_visible_but_not_runnable()
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
        public void User_selected_document_tree_root_is_ready_and_runnable()
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

            Assert.Equal("Ready", item.StatusText);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.True(item.CanPauseSync);
            Assert.True(item.CanStopSync);
        }

        [Fact]
        public void Device_to_cloud_user_selected_document_tree_root_is_ready_and_runnable()
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
        public void Bidirectional_user_selected_document_tree_root_is_ready_and_runnable()
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
