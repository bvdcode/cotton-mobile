namespace Cotton.Mobile.Services
{
    public class CottonDeviceToCloudLocalContentSnapshot
    {
        public CottonDeviceToCloudLocalContentSnapshot(
            string localRootName,
            IReadOnlyList<CottonDeviceToCloudLocalItemSnapshot> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            LocalRootName = string.IsNullOrWhiteSpace(localRootName) ? "Local folder" : localRootName.Trim();
            Items = items;
        }

        public string LocalRootName { get; }

        public IReadOnlyList<CottonDeviceToCloudLocalItemSnapshot> Items { get; }
    }
}
