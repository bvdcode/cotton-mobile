using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.CloudToDeviceSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class CloudToDeviceSyncPlannerTests
    {
        [Fact]
        public void PlannerDownloadsNewRemoteFiles()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.DownloadNewFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.Equal("\"etag-1\"", item.RemoteETag);
            Assert.Equal("alpha.txt", item.RelativePath);
            Assert.Equal(1, plan.DownloadCount);
            Assert.True(plan.HasExecutableChanges);
            Assert.False(plan.HasBlockingItems);
        }

        [Fact]
        public void PlannerKeepsMatchingLocalFile()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonSyncedFileSnapshot localFile = CottonSyncedFileSnapshot.Create(remoteFile, UpdatedAt.AddMinutes(1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.KeepExistingFile, item.Action);
            Assert.True(item.IsNoOp);
            Assert.Equal(1, plan.NoOpCount);
            Assert.False(plan.HasExecutableChanges);
        }

        [Fact]
        public void PlannerRefreshesFileWhenRemoteEtagChanges()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "alpha.txt", "\"etag-2\"");
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt.AddMinutes(-5),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, item.Action);
            Assert.True(item.RequiresDownload);
            Assert.Equal(1, plan.DownloadCount);
        }

        [Fact]
        public void PlannerRefreshesRemoteReplacementAtSameRelativePathWithoutLocalOrphanRemoval()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(SecondFileId, "alpha.txt", "\"etag-2\"");
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt.AddMinutes(-5),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RefreshChangedFile, item.Action);
            Assert.Equal(SecondFileId, item.TargetId);
            Assert.True(item.RequiresDownload);
            Assert.False(item.RemovesLocalFile);
            Assert.Equal(1, plan.DownloadCount);
            Assert.Equal(0, plan.LocalRemovalCount);
        }

        [Fact]
        public void PlannerBlocksRemoteReplacementWithoutEtagWithoutLocalOrphanRemoval()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(SecondFileId, "alpha.txt", eTag: null);
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt.AddMinutes(-5),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.Equal(SecondFileId, item.TargetId);
            Assert.True(item.IsBlocked);
            Assert.Equal(0, plan.LocalRemovalCount);
        }

        [Fact]
        public void PlannerBlocksRemoteFolderReplacementWithoutLocalOrphanRemoval()
        {
            CottonFileBrowserEntry remoteFolder = CreateFolder("alpha.txt");
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt.AddMinutes(-5),
                42,
                "text/plain",
                UpdatedAt.AddMinutes(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFolder),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.BlockedFolder, item.Action);
            Assert.True(item.IsBlocked);
            Assert.Equal(0, plan.LocalRemovalCount);
        }

        [Fact]
        public void PlannerRenamesLocalFileWhenEtagMatchesButNameChanges()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "renamed.txt", "\"etag-1\"");
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt.AddMinutes(1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                CreateContent(remoteFile),
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RenameLocalFile, item.Action);
            Assert.True(item.RequiresLocalRename);
            Assert.Equal("renamed.txt", item.DisplayName);
            Assert.Equal("alpha.txt", item.PreviousRelativePath);
            Assert.True(item.ChangesRelativePath);
            Assert.Equal(1, plan.LocalRenameCount);
        }

        [Fact]
        public void PlannerRenamesLocalFileWhenRelativePathChanges()
        {
            CottonFileBrowserEntry remoteFile = CreateFile(FirstFileId, "alpha.txt", "\"etag-1\"");
            CottonCloudToDeviceRemoteContentSnapshot remote = new(
                FolderId,
                "Projects",
                [new CottonCloudToDeviceRemoteItemSnapshot(remoteFile, "Nested/alpha.txt")]);
            CottonSyncedFileSnapshot localFile = new(
                FirstFileId,
                "alpha.txt",
                "\"etag-1\"",
                UpdatedAt,
                42,
                "text/plain",
                UpdatedAt.AddMinutes(1),
                "alpha.txt");

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                [localFile]);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.RenameLocalFile, item.Action);
            Assert.Equal("Nested/alpha.txt", item.RelativePath);
            Assert.Equal("alpha.txt", item.PreviousRelativePath);
            Assert.True(item.ChangesRelativePath);
            Assert.Equal(1, plan.LocalRenameCount);
        }

        [Fact]
        public void PlannerMarksMissingRemoteEtagAsRefreshRequired()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", eTag: null));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            CottonCloudToDeviceSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonCloudToDeviceSyncActionKind.NeedsFreshServerRevision, item.Action);
            Assert.True(item.IsBlocked);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void PlannerBlocksChildFoldersUntilRecursiveSyncExists()
        {
            CottonFolderContent remote = CreateContent(
                CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""),
                CreateFolder("Archive"));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                []);

            Assert.Equal(2, plan.Items.Count);
            Assert.Contains(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.DownloadNewFile);
            Assert.Contains(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.BlockedFolder);
            Assert.True(plan.HasExecutableChanges);
            Assert.True(plan.HasBlockingItems);
        }

        [Fact]
        public void PlannerRemovesLocalOrphansFromManifest()
        {
            CottonFolderContent remote = CreateContent(CreateFile(FirstFileId, "alpha.txt", "\"etag-1\""));
            CottonSyncedFileSnapshot orphan = new(
                SecondFileId,
                "orphan.txt",
                "\"etag-old\"",
                UpdatedAt.AddDays(-1),
                100,
                "text/plain",
                UpdatedAt.AddHours(-1));

            CottonCloudToDeviceSyncPlanSnapshot plan = CottonCloudToDeviceSyncPlanner.Create(
                CreateReadyRoot(),
                remote,
                [orphan]);

            Assert.Equal(2, plan.Items.Count);
            CottonCloudToDeviceSyncPlanItem removal =
                Assert.Single(plan.Items, item => item.Action == CottonCloudToDeviceSyncActionKind.RemoveLocalOrphan);
            Assert.Equal(SecondFileId, removal.TargetId);
            Assert.True(removal.RemovesLocalFile);
            Assert.Equal(1, plan.LocalRemovalCount);
        }
    }
}
