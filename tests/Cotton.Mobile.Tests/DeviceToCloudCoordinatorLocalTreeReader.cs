using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class DeviceToCloudCoordinatorLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
    {
        private readonly Dictionary<Guid, CottonDeviceToCloudLocalContentSnapshot> _contentByRootId = [];

        public List<Guid> ReadRootIds { get; } = [];

        public void SetContent(Guid rootId, CottonDeviceToCloudLocalContentSnapshot content)
        {
            _contentByRootId[rootId] = content;
        }

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ReadRootIds.Add(root.Id);
            return Task.FromResult(_contentByRootId[root.Id]);
        }
    }
}
