namespace Cotton.Mobile.Services
{
    public class CottonTransferListSnapshot
    {
        private const string EmptyMessageText = "No transfers yet";
        private const string EmptyDetailsText = "Nothing is waiting right now.";

        private CottonTransferListSnapshot(IReadOnlyList<CottonTransferListItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            Items = items;
            SummaryText = FormatSummary(items.Count);
            EmptyMessage = EmptyMessageText;
            EmptyDetails = EmptyDetailsText;
        }

        public IReadOnlyList<CottonTransferListItem> Items { get; }

        public string SummaryText { get; }

        public string EmptyMessage { get; }

        public string EmptyDetails { get; }

        public bool IsEmpty => Items.Count == 0;

        public static CottonTransferListSnapshot Create(IEnumerable<CottonTransferQueueItem> transfers)
        {
            ArgumentNullException.ThrowIfNull(transfers);

            List<CottonTransferListItem> items = transfers
                .OrderByDescending(item => item.UpdatedAtUtc)
                .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(CreateItem)
                .ToList();

            return new CottonTransferListSnapshot(items);
        }

        private static CottonTransferListItem CreateItem(CottonTransferQueueItem item)
        {
            return new CottonTransferListItem(
                item.Id,
                item.DisplayName,
                FormatKind(item.Kind),
                FormatStatus(item.Status),
                FormatProgress(item),
                FormatDetail(item),
                CreateProgressFraction(item.Progress),
                IsProgressVisible(item.Status),
                item.Status == CottonTransferStatus.Failed,
                item.FailureMessage);
        }

        private static string FormatKind(CottonTransferKind kind)
        {
            return kind switch
            {
                CottonTransferKind.Upload => "Upload",
                _ => "Transfer",
            };
        }

        private static string FormatStatus(CottonTransferStatus status)
        {
            return status switch
            {
                CottonTransferStatus.Queued => "Queued",
                CottonTransferStatus.Running => "Uploading",
                CottonTransferStatus.Paused => "Paused",
                CottonTransferStatus.Completed => "Completed",
                CottonTransferStatus.Failed => "Failed",
                CottonTransferStatus.Cancelled => "Cancelled",
                _ => "Unknown",
            };
        }

        private static string FormatProgress(CottonTransferQueueItem item)
        {
            if (item.Status == CottonTransferStatus.Queued)
            {
                return "Waiting";
            }

            CottonTransferProgressSnapshot progress = item.Progress;
            if (progress.TotalBytes is > 0)
            {
                string transferred = CottonFileSizeFormatter.Format(progress.TransferredBytes);
                string total = CottonFileSizeFormatter.Format(progress.TotalBytes.Value);
                return $"{progress.DisplayText} · {transferred} of {total}";
            }

            if (progress.TransferredBytes > 0)
            {
                return $"{progress.DisplayText} transferred";
            }

            return progress.DisplayText;
        }

        private static string FormatDetail(CottonTransferQueueItem item)
        {
            string destination = item.Destination is null ? string.Empty : $" to {item.Destination.Path}";
            return item.Status switch
            {
                CottonTransferStatus.Queued => $"{FormatKind(item.Kind)} waiting{destination}",
                CottonTransferStatus.Running => $"{FormatKind(item.Kind)} in progress{destination}",
                CottonTransferStatus.Paused => $"{FormatKind(item.Kind)} paused{destination}",
                CottonTransferStatus.Completed => $"{FormatKind(item.Kind)} complete{destination}",
                CottonTransferStatus.Failed => $"{FormatKind(item.Kind)} needs attention{destination}",
                CottonTransferStatus.Cancelled => $"{FormatKind(item.Kind)} cancelled{destination}",
                _ => FormatKind(item.Kind),
            };
        }

        private static double CreateProgressFraction(CottonTransferProgressSnapshot progress)
        {
            return progress.Percent.HasValue ? progress.Percent.Value / 100d : 0d;
        }

        private static bool IsProgressVisible(CottonTransferStatus status)
        {
            return status is CottonTransferStatus.Queued
                or CottonTransferStatus.Running
                or CottonTransferStatus.Paused
                or CottonTransferStatus.Failed;
        }

        private static string FormatSummary(int itemCount)
        {
            return itemCount == 1 ? "1 transfer" : $"{itemCount:N0} transfers";
        }
    }
}
