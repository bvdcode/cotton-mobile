using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class BidirectionalLocalTreeReader : ICottonDeviceToCloudLocalTreeReader
    {
        private readonly Dictionary<Guid, CottonDeviceToCloudLocalContentSnapshot> _contentByRootId = [];

        public int ReadCount { get; private set; }

        public void SetContent(Guid rootId, CottonDeviceToCloudLocalContentSnapshot content)
        {
            _contentByRootId[rootId] = content;
        }

        public Task<CottonDeviceToCloudLocalContentSnapshot> ReadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(_contentByRootId[root.Id]);
        }
    }
}
