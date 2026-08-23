using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class DeviceToCloudCoordinatorUploadReceiptStore : ICottonUploadReceiptStore
    {
        private readonly Dictionary<Guid, Dictionary<string, CottonUploadReceiptSnapshot>> _receiptsByRootId = [];

        public List<CottonUploadReceiptSnapshot> SavedReceipts { get; } = [];

        public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_receiptsByRootId.TryGetValue(
                root.Id,
                out Dictionary<string, CottonUploadReceiptSnapshot>? receipts))
            {
                return Task.FromResult<IReadOnlyList<CottonUploadReceiptSnapshot>>([]);
            }

            return Task.FromResult<IReadOnlyList<CottonUploadReceiptSnapshot>>([.. receipts.Values]);
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
                out Dictionary<string, CottonUploadReceiptSnapshot>? receipts))
            {
                receipts = new Dictionary<string, CottonUploadReceiptSnapshot>(StringComparer.Ordinal);
                _receiptsByRootId.Add(root.Id, receipts);
            }

            receipts[receipt.LocalSourceId] = receipt;
            SavedReceipts.Add(receipt);
            return Task.CompletedTask;
        }

        public Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _receiptsByRootId.Remove(root.Id);
            return Task.CompletedTask;
        }

        public Task<int> ClearPendingAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            if (!_receiptsByRootId.TryGetValue(
                    root.Id,
                    out Dictionary<string, CottonUploadReceiptSnapshot>? receipts))
            {
                return Task.FromResult(0);
            }

            string[] pendingSourceIds = [.. receipts.Values
                .Where(receipt => receipt.IsPending)
                .Select(receipt => receipt.LocalSourceId)];
            foreach (string sourceId in pendingSourceIds)
            {
                receipts.Remove(sourceId);
            }

            return Task.FromResult(pendingSourceIds.Length);
        }
    }
}
