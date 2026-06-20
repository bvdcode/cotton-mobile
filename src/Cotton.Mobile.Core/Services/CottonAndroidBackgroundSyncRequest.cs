namespace Cotton.Mobile.Services
{
    public class CottonAndroidBackgroundSyncRequest
    {
        public CottonAndroidBackgroundSyncRequest(Uri instanceUri, int eligibleRootCount)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            if (!instanceUri.IsAbsoluteUri)
            {
                throw new ArgumentException("Instance URI must be absolute.", nameof(instanceUri));
            }

            if (eligibleRootCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(eligibleRootCount),
                    "At least one sync root must be eligible for background sync.");
            }

            InstanceUri = instanceUri;
            EligibleRootCount = eligibleRootCount;
            ScheduleIdentity = CottonAndroidBackgroundSyncScheduleIdentity.Create(instanceUri);
            RequiresNetwork = true;
        }

        public Uri InstanceUri { get; }

        public int EligibleRootCount { get; }

        public CottonAndroidBackgroundSyncScheduleIdentity ScheduleIdentity { get; }

        public bool RequiresNetwork { get; }
    }
}
