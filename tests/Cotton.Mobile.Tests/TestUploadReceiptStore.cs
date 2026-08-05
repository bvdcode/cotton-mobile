using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class TestUploadReceiptStore : ICottonUploadReceiptStore
    {
        private readonly Dictionary<Guid, List<CottonUploadReceiptSnapshot>> _receiptsByRootId = [];

        public List<Guid> ClearedRootIds { get; } = [];

        public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CottonUploadReceiptSnapshot> receipts = _receiptsByRootId.TryGetValue(
                root.Id,
                out List<CottonUploadReceiptSnapshot>? storedReceipts)
                ? storedReceipts.ToArray()
                : [];
            return Task.FromResult(receipts);
        }

        public Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot receipt,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_receiptsByRootId.TryGetValue(
                root.Id,
                out List<CottonUploadReceiptSnapshot>? receipts))
            {
                receipts = [];
                _receiptsByRootId[root.Id] = receipts;
            }

            receipts.RemoveAll(item => item.LocalSourceId == receipt.LocalSourceId);
            receipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (root.Direction != CottonSyncDirection.DeviceToCloud)
            {
                throw new InvalidOperationException("Upload receipts are unsupported for this sync direction.");
            }

            ClearedRootIds.Add(root.Id);
            _receiptsByRootId.Remove(root.Id);
            return Task.CompletedTask;
        }
    }
}
