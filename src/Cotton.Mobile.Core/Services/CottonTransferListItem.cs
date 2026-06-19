namespace Cotton.Mobile.Services
{
    public class CottonTransferListItem
    {
        public CottonTransferListItem(
            Guid id,
            string displayName,
            string kindText,
            string statusText,
            string progressText,
            string detailText,
            double progressFraction,
            bool isProgressVisible,
            bool isFailureVisible,
            string? failureMessage)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Transfer display name is required.", nameof(displayName));
            }

            if (progressFraction is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(progressFraction),
                    "Transfer progress fraction must be between 0 and 1.");
            }

            Id = id;
            DisplayName = displayName.Trim();
            KindText = string.IsNullOrWhiteSpace(kindText) ? "Transfer" : kindText.Trim();
            StatusText = string.IsNullOrWhiteSpace(statusText) ? "Unknown" : statusText.Trim();
            ProgressText = string.IsNullOrWhiteSpace(progressText) ? "Waiting" : progressText.Trim();
            DetailText = string.IsNullOrWhiteSpace(detailText) ? KindText : detailText.Trim();
            ProgressFraction = progressFraction;
            IsProgressVisible = isProgressVisible;
            IsFailureVisible = isFailureVisible && !string.IsNullOrWhiteSpace(failureMessage);
            FailureMessage = IsFailureVisible ? failureMessage!.Trim() : null;
        }

        public Guid Id { get; }

        public string DisplayName { get; }

        public string KindText { get; }

        public string StatusText { get; }

        public string ProgressText { get; }

        public string DetailText { get; }

        public double ProgressFraction { get; }

        public bool IsProgressVisible { get; }

        public bool IsFailureVisible { get; }

        public string? FailureMessage { get; }
    }
}
