using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncRootRunRoutingTests
    {
        private static readonly Uri InstanceUri = new("https://app.cottoncloud.dev");
        private static readonly Guid RootId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        private static readonly Guid FolderId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice, CottonSyncRootRunRoute.CloudToDevice)]
        [InlineData(CottonSyncDirection.DeviceToCloud, CottonSyncRootRunRoute.DeviceToCloud)]
        [InlineData(CottonSyncDirection.Bidirectional, CottonSyncRootRunRoute.Bidirectional)]
        public void Route_matches_sync_direction(CottonSyncDirection direction, CottonSyncRootRunRoute route)
        {
            CottonSyncRootSnapshot root = CreateRoot(direction);

            Assert.Equal(route, CottonSyncRootRunRouting.CreateRoute(direction));
            Assert.Equal(route, CottonSyncRootRunRouting.CreateRoute(root));
        }

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice, "Syncing Projects...")]
        [InlineData(CottonSyncDirection.DeviceToCloud, "Syncing Projects...")]
        [InlineData(CottonSyncDirection.Bidirectional, "Syncing Projects both ways...")]
        public void Starting_status_matches_sync_direction(CottonSyncDirection direction, string expected)
        {
            CottonSyncRootSnapshot root = CreateRoot(direction);

            Assert.Equal(expected, CottonSyncRootRunRouting.CreateStartingStatus(root));
        }

        [Theory]
        [InlineData(CottonSyncDirection.CloudToDevice)]
        [InlineData(CottonSyncDirection.DeviceToCloud)]
        [InlineData(CottonSyncDirection.Bidirectional)]
        public void Offline_and_failed_statuses_are_routed_by_direction(CottonSyncDirection direction)
        {
            Assert.Equal(
                "Offline. Sync needs internet.",
                CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(direction));
            Assert.Equal("Sync failed.", CottonSyncRootRunRouting.CreateFailedStatus(direction));
        }

        [Fact]
        public void Unknown_direction_is_rejected()
        {
            var direction = (CottonSyncDirection)999;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonSyncRootRunRouting.CreateRoute(direction));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonSyncRootRunRouting.CreateOfflineUnavailableStatus(direction));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => CottonSyncRootRunRouting.CreateFailedStatus(direction));
        }

        [Fact]
        public void Null_root_is_rejected()
        {
            Assert.Throws<ArgumentNullException>(
                () => CottonSyncRootRunRouting.CreateRoute(root: null!));
            Assert.Throws<ArgumentNullException>(
                () => CottonSyncRootRunRouting.CreateStartingStatus(root: null!));
        }

        private static CottonSyncRootSnapshot CreateRoot(CottonSyncDirection direction)
        {
            return new CottonSyncRootSnapshot(
                RootId,
                InstanceUri,
                "user:mobile-demo",
                new CottonUploadDestinationSnapshot(FolderId, "Projects", "Files / Projects"),
                new CottonSyncLocalRootSnapshot(
                    CottonSyncRootStorageKind.UserSelectedDocumentTree,
                    "content://tree/projects",
                    "Projects",
                    CottonSyncRootPermissionStatus.Available),
                direction);
        }
    }
}
