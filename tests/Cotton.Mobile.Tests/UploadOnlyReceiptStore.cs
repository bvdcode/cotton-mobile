using Cotton.Mobile.Services;

namespace Cotton.Mobile.Tests
{
    internal class UploadOnlyReceiptStore : ICottonUploadReceiptStore
    {
        private readonly List<string> _events;

        public UploadOnlyReceiptStore(
            List<string> events,
            IReadOnlyList<CottonUploadReceiptSnapshot> initialReceipts)
        {
            _events = events;
            Receipts = [.. initialReceipts];
        }

        public List<CottonUploadReceiptSnapshot> Receipts { get; }

        public List<CottonUploadReceiptSnapshot> SaveHistory { get; } = [];

        public int PendingSaveCount { get; private set; }

        public int UploadedSaveCount { get; private set; }

        public bool ThrowOnUploadedSave { get; set; }

        public Task<IReadOnlyList<CottonUploadReceiptSnapshot>> LoadAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            _events.Add("receipt:load");
            IReadOnlyList<CottonUploadReceiptSnapshot> receipts = [.. Receipts];
            return Task.FromResult(receipts);
        }

        public Task SaveAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CottonUploadReceiptSnapshot receipt,
            CancellationToken cancellationToken = default)
        {
            SaveHistory.Add(receipt);
            switch (receipt.Status)
            {
                case CottonUploadReceiptStatus.Pending:
                    PendingSaveCount++;
                    _events.Add("receipt:pending");
                    break;

                case CottonUploadReceiptStatus.Uploaded:
                    UploadedSaveCount++;
                    _events.Add("receipt:uploaded");
                    if (ThrowOnUploadedSave)
                    {
                        return Task.FromException(new IOException("Uploaded receipt write failed."));
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(receipt), "Receipt status is not supported.");
            }

            int existingIndex = Receipts.FindIndex(existing =>
                string.Equals(existing.LocalSourceId, receipt.LocalSourceId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                Receipts[existingIndex] = receipt;
            }
            else
            {
                Receipts.Add(receipt);
            }

            return Task.CompletedTask;
        }

        public Task ClearAsync(
            Uri instanceUri,
            CottonSyncRootSnapshot root,
            CancellationToken cancellationToken = default)
        {
            Receipts.Clear();
            return Task.CompletedTask;
        }
    }
}
