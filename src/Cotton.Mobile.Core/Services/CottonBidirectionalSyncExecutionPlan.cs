namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncExecutionPlan
    {
        public CottonBidirectionalSyncExecutionPlan(
            CottonBidirectionalSyncPlanSnapshot preflightPlan,
            CottonCloudToDeviceSyncPlanSnapshot cloudToDevicePlan,
            CottonDeviceToCloudSyncPlanSnapshot deviceToCloudPlan)
        {
            ArgumentNullException.ThrowIfNull(preflightPlan);
            ArgumentNullException.ThrowIfNull(cloudToDevicePlan);
            ArgumentNullException.ThrowIfNull(deviceToCloudPlan);

            if (preflightPlan.SyncRootId != cloudToDevicePlan.SyncRootId
                || preflightPlan.SyncRootId != deviceToCloudPlan.SyncRootId)
            {
                throw new ArgumentException("Bidirectional execution plans must belong to the same sync root.");
            }

            if (preflightPlan.FolderId != cloudToDevicePlan.FolderId
                || preflightPlan.FolderId != deviceToCloudPlan.FolderId)
            {
                throw new ArgumentException("Bidirectional execution plans must belong to the same cloud folder.");
            }

            PreflightPlan = preflightPlan;
            CloudToDevicePlan = cloudToDevicePlan;
            DeviceToCloudPlan = deviceToCloudPlan;
        }

        public CottonBidirectionalSyncPlanSnapshot PreflightPlan { get; }

        public CottonCloudToDeviceSyncPlanSnapshot CloudToDevicePlan { get; }

        public CottonDeviceToCloudSyncPlanSnapshot DeviceToCloudPlan { get; }

        public bool CanExecute => !PreflightPlan.HasBlockingItems;

        public bool HasCloudToDeviceWork => CloudToDevicePlan.HasExecutableChanges;

        public bool HasDeviceToCloudWork => DeviceToCloudPlan.HasExecutableChanges;

        public bool HasExecutableChanges => HasCloudToDeviceWork || HasDeviceToCloudWork;

        public bool HasDestructiveChanges => PreflightPlan.HasDestructiveChanges;

        public int BlockedCount => PreflightPlan.BlockedCount;
    }
}
