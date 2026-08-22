using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class UploadOnlyLocalFileOperator(List<string> events) : ICottonDeviceToCloudLocalFileOperator
    {
        public CottonDeviceToCloudLocalFileDeleteStatus DeleteStatus { get; set; } =
            CottonDeviceToCloudLocalFileDeleteStatus.Deleted;

        public List<CottonDeviceToCloudSyncPlanItem> DeleteCalls { get; } = [];

        public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(item);
            events.Add("local:delete");
            return Task.FromResult(DeleteStatus);
        }
    }
}
