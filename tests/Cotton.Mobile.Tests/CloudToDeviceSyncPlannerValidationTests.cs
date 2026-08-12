using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.CloudToDeviceSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPlannerValidationTests
    {
        [Fact]
        public void PlannerRejectsWrongCloudFolder()
        {
            CottonFolderContent remote = new(
                Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                "Other",
                [CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"")]);

            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(CreateReadyRoot(), remote, []));
        }

        [Fact]
        public void PlannerRejectsNotReadyRoot()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));

            Assert.Throws<InvalidOperationException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateRoot(CottonSyncRootPermissionStatus.Unavailable),
                    remote,
                    []));
        }

        [Fact]
        public void PlannerRejectsNonCloudToDeviceRoots()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));

            Assert.Throws<InvalidOperationException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateRoot(CottonSyncRootPermissionStatus.Available, CottonSyncDirection.DeviceToCloud),
                    remote,
                    []));
            Assert.Throws<InvalidOperationException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateRoot(CottonSyncRootPermissionStatus.Available, CottonSyncDirection.Bidirectional),
                    remote,
                    []));
        }

        [Fact]
        public void PlannerRejectsDuplicateManifestOrRemoteFileIds()
        {
            CottonSyncedFileSnapshot first = CottonSyncedFileSnapshot.Create(
                CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""),
                UpdatedAt);
            CottonSyncedFileSnapshot duplicate = new(
                FirstFileId,
                "duplicate.txt",
                "\"etag-2\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt);

            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(CreateReadyRoot(), CreateContent(), [first, duplicate]));
            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateContent(),
                    [
                        first,
                        new CottonSyncedFileSnapshot(
                            SecondFileId,
                            "alpha.txt",
                            "\"etag-2\"",
                            UpdatedAt,
                            42,
                            "text/plain",
                            UpdatedAt),
                    ]));
            Assert.Throws<ArgumentException>(() =>
                CottonCloudToDeviceSyncPlanner.Create(
                    CreateReadyRoot(),
                    CreateContent(
                        CreateFile(ThirdFileId, "first.txt", "\"etag-1\""),
                        CreateFile(ThirdFileId, "second.txt", "\"etag-2\"")),
                    []));
        }
    }
}
