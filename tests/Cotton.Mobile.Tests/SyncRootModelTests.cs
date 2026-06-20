using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootModelTests
    {
        private static readonly Guid SyncRootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid CloudFolderId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        [Fact]
        public void App_private_sync_root_is_ready_without_user_folder_grant()
        {
            CottonSyncLocalRootSnapshot localRoot = CreateAppPrivateLocalRoot();

            CottonSyncRootSnapshot root = CreateRoot(localRoot, CottonSyncDirection.CloudToDevice);

            Assert.True(localRoot.UsesAppPrivateStorage);
            Assert.False(localRoot.RequiresPersistedUserGrant);
            Assert.True(localRoot.CanReadWrite);
            Assert.Equal(CottonSyncRootReadinessStatus.Ready, root.ReadinessStatus);
            Assert.True(root.CanRunSync);
            Assert.False(root.NeedsUserAction);
            Assert.Equal("Sync root ready", root.StatusText);
        }

        [Fact]
        public void User_selected_document_tree_requires_explicit_grant_before_sync_can_run()
        {
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/primary%3ACotton",
                "Cotton",
                CottonSyncRootPermissionStatus.NeedsUserGrant);

            CottonSyncRootSnapshot root = CreateRoot(localRoot, CottonSyncDirection.DeviceToCloud);

            Assert.True(localRoot.RequiresPersistedUserGrant);
            Assert.False(localRoot.CanReadWrite);
            Assert.Equal(CottonSyncRootReadinessStatus.NeedsUserGrant, root.ReadinessStatus);
            Assert.False(root.CanRunSync);
            Assert.True(root.NeedsUserAction);
            Assert.Equal("Choose local folder", root.StatusText);
        }

        [Fact]
        public void Revoked_document_tree_surfaces_reconnect_state()
        {
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.UserSelectedDocumentTree,
                "content://tree/primary%3ACotton",
                "Cotton",
                CottonSyncRootPermissionStatus.Revoked);

            CottonSyncRootSnapshot root = CreateRoot(localRoot, CottonSyncDirection.Bidirectional);

            Assert.Equal(CottonSyncRootReadinessStatus.GrantRevoked, root.ReadinessStatus);
            Assert.True(root.NeedsUserAction);
            Assert.False(root.CanRunSync);
            Assert.True(root.IsBidirectional);
            Assert.Equal("Reconnect local folder", root.StatusText);
        }

        [Fact]
        public void Unavailable_local_root_blocks_sync_without_user_action()
        {
            var localRoot = new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-sync-root",
                "On this device",
                CottonSyncRootPermissionStatus.Unavailable);

            CottonSyncRootSnapshot root = CreateRoot(localRoot, CottonSyncDirection.CloudToDevice);

            Assert.Equal(CottonSyncRootReadinessStatus.LocalRootUnavailable, root.ReadinessStatus);
            Assert.False(root.CanRunSync);
            Assert.False(root.NeedsUserAction);
            Assert.Equal("Local folder unavailable", root.StatusText);
        }

        [Fact]
        public void Stable_key_normalizes_instance_uri_and_binds_account_cloud_folder_and_local_root()
        {
            CottonSyncLocalRootSnapshot localRoot = CreateAppPrivateLocalRoot();

            CottonSyncRootSnapshot root = CreateRoot(
                new Uri("HTTPS://APP.COTTONCLOUD.DEV:443/mobile/"),
                " account-1 ",
                localRoot);
            CottonSyncRootSnapshot equivalentRoot = CreateRoot(
                new Uri("https://app.cottoncloud.dev/mobile"),
                "account-1",
                localRoot);
            CottonSyncRootSnapshot differentAccountRoot = CreateRoot(
                new Uri("https://app.cottoncloud.dev/mobile"),
                "account-2",
                localRoot);

            Assert.Equal("https://app.cottoncloud.dev/mobile", root.InstanceUri.AbsoluteUri.TrimEnd('/'));
            Assert.Equal(64, root.StableKey.Length);
            Assert.Equal(root.StableKey, equivalentRoot.StableKey);
            Assert.NotEqual(root.StableKey, differentAccountRoot.StableKey);
        }

        [Fact]
        public void Local_root_rejects_app_private_user_grant_state()
        {
            Assert.Throws<ArgumentException>(() =>
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.AppPrivateDirectory,
                    "app-private-sync-root",
                    "On this device",
                    CottonSyncRootPermissionStatus.NeedsUserGrant));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Root_requires_account_scope_key(string accountScopeKey)
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoot(new Uri("https://app.cottoncloud.dev"), accountScopeKey, CreateAppPrivateLocalRoot()));
        }

        [Theory]
        [InlineData("https://app.cottoncloud.dev?debug=true")]
        [InlineData("https://app.cottoncloud.dev#files")]
        [InlineData("http://app.cottoncloud.dev")]
        [InlineData("ftp://app.cottoncloud.dev")]
        public void Root_rejects_unsupported_instance_uri(string value)
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRoot(new Uri(value), "account-1", CreateAppPrivateLocalRoot()));
        }

        private static CottonSyncRootSnapshot CreateRoot(
            CottonSyncLocalRootSnapshot localRoot,
            CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                new Uri("https://app.cottoncloud.dev"),
                "account-1",
                CreateCloudFolder(),
                localRoot,
                direction);
        }

        private static CottonSyncRootSnapshot CreateRoot(
            Uri instanceUri,
            string accountScopeKey,
            CottonSyncLocalRootSnapshot localRoot)
        {
            return new CottonSyncRootSnapshot(
                SyncRootId,
                instanceUri,
                accountScopeKey,
                CreateCloudFolder(),
                localRoot,
                CottonSyncDirection.CloudToDevice);
        }

        private static CottonSyncLocalRootSnapshot CreateAppPrivateLocalRoot()
        {
            return new CottonSyncLocalRootSnapshot(
                CottonSyncRootStorageKind.AppPrivateDirectory,
                "app-private-sync-root",
                "On this device",
                CottonSyncRootPermissionStatus.Available);
        }

        private static CottonUploadDestinationSnapshot CreateCloudFolder()
        {
            return new CottonUploadDestinationSnapshot(
                CloudFolderId,
                "Projects",
                "Files / Projects");
        }
    }
}
