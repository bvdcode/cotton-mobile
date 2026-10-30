using Cotton.Mobile.Services;
using Xunit;

namespace Cotton.Mobile.Tests
{
    public class SyncExecutionWorkflowTests
    {
        [Fact]
        public async Task RunRootUsesDeviceToCloudCoordinator()
        {
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            SyncExecutionWorkflow workflow = new(coordinator);

            string status = await workflow.RunRootAsync(
                SyncTestRootFactory.InstanceUri,
                SyncTestRootFactory.CreateDocumentTreeRoot());

            Assert.Equal(1, coordinator.RunRootCount);
            Assert.Equal("No folders are set to sync.", status);
        }

        [Fact]
        public async Task RunAllRunsEveryConfiguredRoot()
        {
            RecordingDeviceToCloudSyncCoordinator coordinator = new();
            SyncExecutionWorkflow workflow = new(coordinator);
            IReadOnlyList<CottonSyncRootSnapshot> roots =
            [
                SyncTestRootFactory.CreateDocumentTreeRoot(),
                SyncTestRootFactory.CreateMediaStoreRoot(),
            ];

            string status = await workflow.RunAllAsync(
                SyncTestRootFactory.InstanceUri,
                roots);

            Assert.Equal(2, coordinator.RunRootCount);
            Assert.Equal("No folders are set to sync.", status);
        }
    }
}
