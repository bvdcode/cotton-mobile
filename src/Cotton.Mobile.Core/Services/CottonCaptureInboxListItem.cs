namespace Cotton.Mobile.Services
{
    public class CottonCaptureInboxListItem
    {
        public CottonCaptureInboxListItem(
            Guid intakeId,
            Guid itemId,
            string displayName,
            string kindText,
            string statusText,
            string detailText,
            string metadataText,
            bool isFailureVisible,
            string? failureMessage)
        {
            if (intakeId == Guid.Empty)
            {
                throw new ArgumentException("Capture inbox intake id cannot be empty.", nameof(intakeId));
            }

            if (itemId == Guid.Empty)
            {
                throw new ArgumentException("Capture inbox item id cannot be empty.", nameof(itemId));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Capture inbox display name is required.", nameof(displayName));
            }

            IntakeId = intakeId;
            ItemId = itemId;
            DisplayName = displayName.Trim();
            KindText = string.IsNullOrWhiteSpace(kindText) ? "Shared item" : kindText.Trim();
            StatusText = string.IsNullOrWhiteSpace(statusText) ? "Unknown" : statusText.Trim();
            DetailText = string.IsNullOrWhiteSpace(detailText) ? KindText : detailText.Trim();
            MetadataText = string.IsNullOrWhiteSpace(metadataText) ? StatusText : metadataText.Trim();
            IsFailureVisible = isFailureVisible && !string.IsNullOrWhiteSpace(failureMessage);
            FailureMessage = IsFailureVisible ? failureMessage!.Trim() : null;
        }

        public Guid IntakeId { get; }

        public Guid ItemId { get; }

        public string DisplayName { get; }

        public string KindText { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public string MetadataText { get; }

        public bool IsFailureVisible { get; }

        public string? FailureMessage { get; }
    }
}
