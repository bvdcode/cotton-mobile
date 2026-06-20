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
            Assert.Equal("Projects", item.Title);
            Assert.Equal("Files / Projects", item.PathText);
            Assert.Equal("Cloud to device · On this device", item.DetailText);
            Assert.Equal("Sync root ready", item.StatusText);
            Assert.True(item.IsReady);
            Assert.False(item.IsAttentionVisible);
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
            Assert.Equal("Device to cloud · On this device", state.Items[0].DetailText);
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
