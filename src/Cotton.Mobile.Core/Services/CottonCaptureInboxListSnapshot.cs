namespace Cotton.Mobile.Services
{
    public class CottonCaptureInboxListSnapshot
    {
        private const string EmptyMessageText = "No captured items";
        private const string EmptyDetailsText = "Shared files and text will appear here.";

        private CottonCaptureInboxListSnapshot(IReadOnlyList<CottonCaptureInboxListItem> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            Items = items;
            SummaryText = FormatSummary(items.Count);
            EmptyMessage = EmptyMessageText;
            EmptyDetails = EmptyDetailsText;
        }

        public IReadOnlyList<CottonCaptureInboxListItem> Items { get; }

        public string SummaryText { get; }

        public string EmptyMessage { get; }

        public string EmptyDetails { get; }

        public bool IsEmpty => Items.Count == 0;

        public static CottonCaptureInboxListSnapshot Create(
            IEnumerable<CottonShareIntakeSnapshot> inboxSnapshots)
        {
            ArgumentNullException.ThrowIfNull(inboxSnapshots);

            List<CottonCaptureInboxListItem> items = inboxSnapshots
                .OrderByDescending(snapshot => snapshot.ReceivedAtUtc)
                .ThenBy(snapshot => snapshot.Id)
                .SelectMany(CreateItems)
                .ToList();
            return new CottonCaptureInboxListSnapshot(items);
        }

        private static IEnumerable<CottonCaptureInboxListItem> CreateItems(CottonShareIntakeSnapshot snapshot)
        {
            foreach (CottonShareIntakeItemSnapshot item in snapshot.Items)
            {
                yield return new CottonCaptureInboxListItem(
                    snapshot.Id,
                    item.Id,
                    FormatDisplayName(item),
                    FormatKind(item),
                    FormatStatus(snapshot, item),
                    FormatDetail(snapshot, item),
                    FormatMetadata(snapshot, item),
                    FormatDestination(snapshot, item),
                    IsDestinationVisible(snapshot, item),
                    CanSelectDestination(snapshot, item),
                    IsFailureVisible(snapshot, item),
                    FormatFailure(snapshot, item));
            }
        }

        private static string FormatDisplayName(CottonShareIntakeItemSnapshot item)
        {
            if (!string.IsNullOrWhiteSpace(item.DisplayName))
            {
                return item.DisplayName;
            }

            if (item.Type == CottonShareIntakeItemType.Text)
            {
                return item.Value.Length <= 48 ? item.Value : $"{item.Value[..48].Trim()}...";
            }

            return "Shared file";
        }

        private static string FormatKind(CottonShareIntakeItemSnapshot item)
        {
            return item.Type switch
            {
                CottonShareIntakeItemType.Text => "Text",
                CottonShareIntakeItemType.Uri => item.HasStagedContent ? "File" : "Link",
                _ => "Shared item",
            };
        }

        private static string FormatStatus(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            if (snapshot.Status == CottonShareIntakeStatus.MissingPermission)
            {
                return "Needs access";
            }

            if (snapshot.Status == CottonShareIntakeStatus.Unsupported)
            {
                return "Unsupported";
            }

            if (item.Type == CottonShareIntakeItemType.Uri && !item.HasStagedContent)
            {
                return "Not staged";
            }

            if (item.Type == CottonShareIntakeItemType.Uri
                && item.HasStagedContent
                && snapshot.Destination is null)
            {
                return "Choose folder";
            }

            return "Ready";
        }

        private static string FormatDetail(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            if (snapshot.Status == CottonShareIntakeStatus.MissingPermission)
            {
                return "Android access was not granted";
            }

            if (snapshot.Status == CottonShareIntakeStatus.Unsupported)
            {
                return "Could not copy this item";
            }

            if (item.Type == CottonShareIntakeItemType.Text)
            {
                return "Text share captured";
            }

            return item.HasStagedContent ? "Copied to this device" : "Waiting for file access";
        }

        private static string FormatMetadata(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            List<string> parts = [];
            if (item.StagedSizeBytes is long sizeBytes)
            {
                parts.Add(CottonFileSizeFormatter.Format(sizeBytes));
            }

            string? mimeType = item.MimeType ?? snapshot.SourceMimeType;
            if (!string.IsNullOrWhiteSpace(mimeType))
            {
                parts.Add(mimeType);
            }

            parts.Add(snapshot.ReceivedAtUtc.ToLocalTime().ToString("g"));
            return string.Join(" · ", parts);
        }

        private static string FormatDestination(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            if (!IsDestinationVisible(snapshot, item))
            {
                return string.Empty;
            }

            return snapshot.Destination is null
                ? "No destination selected"
                : $"Destination: {snapshot.Destination.Path}";
        }

        private static bool IsDestinationVisible(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            return snapshot.Status == CottonShareIntakeStatus.Pending
                && item.Type == CottonShareIntakeItemType.Uri
                && item.HasStagedContent;
        }

        private static bool CanSelectDestination(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            return IsDestinationVisible(snapshot, item);
        }

        private static bool IsFailureVisible(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            return snapshot.Status != CottonShareIntakeStatus.Pending
                || (item.Type == CottonShareIntakeItemType.Uri && !item.HasStagedContent);
        }

        private static string? FormatFailure(
            CottonShareIntakeSnapshot snapshot,
            CottonShareIntakeItemSnapshot item)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.FailureMessage))
            {
                return snapshot.FailureMessage;
            }

            if (item.Type == CottonShareIntakeItemType.Uri && !item.HasStagedContent)
            {
                return "The shared file is not available on this device yet.";
            }

            return null;
        }

        private static string FormatSummary(int itemCount)
        {
            return itemCount == 1 ? "1 captured item" : $"{itemCount:N0} captured items";
        }
    }
}
