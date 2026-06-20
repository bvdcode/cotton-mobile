namespace Cotton.Mobile.Services
{
    public class CottonPermissionLedgerItem
    {
        public CottonPermissionLedgerItem(
            string title,
            string statusText,
            string detailText,
            bool needsAttention)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(statusText);
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            Title = title.Trim();
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
            NeedsAttention = needsAttention;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool NeedsAttention { get; }
    }
}
