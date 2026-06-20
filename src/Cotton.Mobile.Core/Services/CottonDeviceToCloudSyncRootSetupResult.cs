namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudSyncRootSetupResult
    {
        public CottonDeviceToCloudSyncRootSetupResult(
            CottonDeviceToCloudSyncRootSetupStatus status,
            CottonSyncRootSnapshot root)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Device-to-cloud sync root setup status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(root);

            Status = status;
            Root = root;
        }

        public CottonDeviceToCloudSyncRootSetupStatus Status { get; }

        public CottonSyncRootSnapshot Root { get; }

        public bool Created => Status == CottonDeviceToCloudSyncRootSetupStatus.Created;

        public bool AlreadyConfigured => Status == CottonDeviceToCloudSyncRootSetupStatus.AlreadyConfigured;

        public bool Updated => Status == CottonDeviceToCloudSyncRootSetupStatus.Updated;

        public bool DirectionConflict => Status == CottonDeviceToCloudSyncRootSetupStatus.DirectionConflict;

        public bool Enabled => Created || AlreadyConfigured || Updated;
    }
}
