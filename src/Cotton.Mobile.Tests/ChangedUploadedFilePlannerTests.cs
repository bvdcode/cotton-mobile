using Cotton.Mobile.Services;
using Xunit;
using static Cotton.Mobile.Tests.DeviceToCloudSyncPlannerTestData;

namespace Cotton.Mobile.Tests
{
    public class ChangedUploadedFilePlannerTests
    {
        [Theory]
        [InlineData(CottonUploadOriginalRetention.KeepOriginals)]
        [InlineData(CottonUploadOriginalRetention.DeleteAfterConfirmedUpload)]
        public void ChangedUploadedFilePreservesBothCopiesAndDoesNotBecomePending(
            CottonUploadOriginalRetention retention)
        {
            CottonDeviceToCloudSyncPlanSnapshot plan = CottonDeviceToCloudSyncPlanner.Create(
                CreateReadyRoot(retention),
                CreateLocalContent(CreateLocalFile("alpha.txt", "alpha.txt", SyncedAt.AddMinutes(2), 60,
                    "document-alpha", TestContentHashes.Second)),
                CreateRemoteContent(CreateRemoteFile(FirstFileId, "alpha.txt", "alpha.txt", "\"etag-1\"")),
                [CreateUploadedReceipt()]);

            CottonDeviceToCloudSyncPlanItem item = Assert.Single(plan.Items);
            Assert.Equal(CottonDeviceToCloudSyncActionKind.UploadedLocalVersionChanged, item.Action);
            Assert.Equal(FirstFileId, item.CloudItemId);
            Assert.True(item.IsBlocked);
            Assert.False(item.RequiresServerMutation);
            Assert.False(item.RequiresLocalMutation);
            Assert.False(plan.HasExecutableChanges);
        }
    }
}
