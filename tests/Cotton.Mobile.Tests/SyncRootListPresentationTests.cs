using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootListPresentationTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid FirstRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid SecondRootId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        private static readonly Guid FirstFolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        private static readonly Guid SecondFolderId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        [Fact]
        public void Empty_state_is_explicit()
        {
            CottonSyncRootListDisplayState state = CottonSyncRootListDisplayState.Create([]);

            Assert.Empty(state.Items);
            Assert.Equal("No folders syncing", state.SummaryText);
            Assert.True(state.IsEmptyVisible);
            Assert.False(state.HasItems);
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

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal(FirstRootId, item.Id);
            Assert.Equal(CottonSyncDirection.CloudToDevice, item.Direction);
            Assert.Equal("Projects", item.Title);
            Assert.Equal("Files / Projects", item.PathText);
            Assert.Equal("Cloud to device · On this device", item.DetailText);
            Assert.Equal("Sync root ready", item.StatusText);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.Equal("Run now", item.RunNowActionText);
            Assert.False(item.IsPaused);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.CanPauseSync);
            Assert.Equal("Pause", item.PauseSyncActionText);
            Assert.False(item.CanResumeSync);
            Assert.Equal("Resume", item.ResumeSyncActionText);
            Assert.True(item.CanStopSync);
            Assert.Equal("Stop syncing", item.StopSyncActionText);
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

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal("Choose local folder", item.StatusText);
            Assert.False(item.IsReady);
            Assert.True(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
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
            Assert.Equal(["Projects", "Archive"], state.Items.Select(item => item.Title).ToArray());
            Assert.Equal(CottonSyncDirection.DeviceToCloud, state.Items[0].Direction);
            Assert.Equal("Device to cloud · On this device", state.Items[0].DetailText);
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

            CottonSyncRootListItem item =
                Assert.Single(CottonSyncRootListDisplayState.Create([root], new HashSet<Guid> { root.Id }).Items);

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

            Assert.Equal("Sync root ready", item.StatusText);
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

            Assert.Equal("Device to cloud · Device folder", item.DetailText);
            Assert.Equal("Sync root ready", item.StatusText);
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
            Assert.Equal("Sync root ready", item.StatusText);
            Assert.False(item.IsUnsupportedLocalRoot);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
            Assert.True(item.CanRunNow);
            Assert.True(item.CanPauseSync);
            Assert.True(item.CanStopSync);
        }

        [Fact]
        public void Device_to_cloud_app_private_root_shows_source_unsupported()
        {
            CottonSyncRootSnapshot root = CreateRoot(
                FirstRootId,
                FirstFolderId,
                "Camera",
                "Files / Camera",
                CottonSyncRootPermissionStatus.Available,
                CottonSyncDirection.DeviceToCloud);

            CottonSyncRootListItem item = Assert.Single(CottonSyncRootListDisplayState.Create([root]).Items);

            Assert.Equal("Local sync source unsupported", item.StatusText);
            Assert.True(item.IsUnsupportedLocalRoot);
            Assert.False(item.IsReady);
            Assert.True(item.IsAttentionVisible);
            Assert.False(item.CanRunNow);
        }

        [Fact]
        public void Stop_sync_management_copy_is_explicit_about_local_files()
        {
            Assert.Equal("Stop syncing Projects?", CottonSyncRootManagementText.CreateStopTitle(" Projects "));
            Assert.Equal(
                "This stops future sync for this folder. Files already on this device are not deleted.",
                CottonSyncRootManagementText.StopMessage);
            Assert.Equal("Stopped syncing Projects.", CottonSyncRootManagementText.CreateStoppedStatus("Projects"));
            Assert.Equal("Sync folder is no longer configured.", CottonSyncRootManagementText.RootMissingStatus);
            Assert.Equal("Sync paused. Resume this folder first.", CottonSyncRootManagementText.RootPausedStatus);
            Assert.Equal("Could not pause syncing this folder.", CottonSyncRootManagementText.PauseFailedStatus);
            Assert.Equal("Could not resume syncing this folder.", CottonSyncRootManagementText.ResumeFailedStatus);
            Assert.Equal("Could not stop syncing this folder.", CottonSyncRootManagementText.StopFailedStatus);
            Assert.Equal("Paused syncing Projects.", CottonSyncRootManagementText.CreatePausedStatus("Projects"));
            Assert.Equal("Resumed syncing Projects.", CottonSyncRootManagementText.CreateResumedStatus("Projects"));
        }

        [Fact]
        public void Stop_sync_management_copy_handles_blank_folder_name()
        {
            Assert.Equal("Stop syncing this folder?", CottonSyncRootManagementText.CreateStopTitle(" "));
            Assert.Equal("Stopped syncing this folder.", CottonSyncRootManagementText.CreateStoppedStatus(string.Empty));
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Guid rootId,
            Guid folderId,
            string folderName,
            string path,
            CottonSyncRootPermissionStatus permissionStatus,
            CottonSyncDirection direction,
            CottonSyncRootStorageKind storageKind = CottonSyncRootStorageKind.AppPrivateDirectory,
            string displayName = "On this device")
        {
            return new CottonSyncRootSnapshot(
                rootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(folderId, folderName, path),
                new CottonSyncLocalRootSnapshot(
                    storageKind,
                    "app-private-cloud-to-device",
                    displayName,
                    permissionStatus),
                direction);
        }
    }
}
