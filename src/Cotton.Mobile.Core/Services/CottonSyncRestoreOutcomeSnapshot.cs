namespace Cotton.Mobile.Services
{
    public class CottonSyncRestoreOutcomeSnapshot
    {
        public CottonSyncRestoreOutcomeSnapshot(
            CottonSyncRestoreOutcomeStatus status,
            bool canRetryWithCreateMissingParents,
            bool canRetryWithOverwrite,
            string summaryText)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Restore outcome status is not supported.");
            }

            Status = status;
            CanRetryWithCreateMissingParents = canRetryWithCreateMissingParents;
            CanRetryWithOverwrite = canRetryWithOverwrite;
            SummaryText = string.IsNullOrWhiteSpace(summaryText)
                ? throw new ArgumentException("Summary text is required.", nameof(summaryText))
                : summaryText;
        }

        public CottonSyncRestoreOutcomeStatus Status { get; }

        public bool CanRetryWithCreateMissingParents { get; }

        public bool CanRetryWithOverwrite { get; }

        public string SummaryText { get; }

        public bool IsTerminal => Status is CottonSyncRestoreOutcomeStatus.Restored
            or CottonSyncRestoreOutcomeStatus.NotRestorable;
    }
}
