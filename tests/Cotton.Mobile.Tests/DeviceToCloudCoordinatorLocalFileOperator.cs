using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class DeviceToCloudCoordinatorLocalFileOperator : ICottonDeviceToCloudLocalFileOperator
    {
        public Task<CottonDeviceToCloudLocalFileDeleteStatus> DeleteIfUnchangedAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonDeviceToCloudSyncPlanItem item,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CottonDeviceToCloudLocalFileDeleteStatus.Unsupported);
        }
    }
}
