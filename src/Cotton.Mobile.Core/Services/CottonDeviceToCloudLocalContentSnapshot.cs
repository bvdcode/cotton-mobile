namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudLocalContentSnapshot
    {
        public CottonDeviceToCloudLocalContentSnapshot(
            string localRootName,
            IReadOnlyList<CottonDeviceToCloudLocalItemSnapshot> items,
            IReadOnlyList<CottonDeviceToCloudLocalProblemSnapshot>? problems = null)
        {
            ArgumentNullException.ThrowIfNull(items);

            LocalRootName = string.IsNullOrWhiteSpace(localRootName) ? "Local folder" : localRootName.Trim();
            Items = items;
            Problems = problems ?? [];
        }

        public string LocalRootName { get; }

        public IReadOnlyList<CottonDeviceToCloudLocalItemSnapshot> Items { get; }

        public IReadOnlyList<CottonDeviceToCloudLocalProblemSnapshot> Problems { get; }
    }
}
