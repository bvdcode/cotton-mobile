namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncRootSetupResult
    {
        public CottonBidirectionalSyncRootSetupResult(
            CottonBidirectionalSyncRootSetupStatus status,
            CottonSyncRootSnapshot root)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Bidirectional sync root setup status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(root);

            Status = status;
            Root = root;
        }

        public CottonBidirectionalSyncRootSetupStatus Status { get; }

        public CottonSyncRootSnapshot Root { get; }

        public bool Created => Status == CottonBidirectionalSyncRootSetupStatus.Created;

        public bool AlreadyConfigured => Status == CottonBidirectionalSyncRootSetupStatus.AlreadyConfigured;

        public bool Updated => Status == CottonBidirectionalSyncRootSetupStatus.Updated;

        public bool Enabled => Created || AlreadyConfigured || Updated;
    }
}
