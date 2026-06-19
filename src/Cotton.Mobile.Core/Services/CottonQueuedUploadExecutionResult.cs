namespace Cotton.Mobile.Services
{
    public class CottonQueuedUploadExecutionResult
    {
        public CottonQueuedUploadExecutionResult(
            CottonQueuedUploadExecutionStatus status,
            CottonTransferQueueItem? transfer,
            string? failureMessage)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Queued upload execution status is not supported.");
            }

            Status = status;
            Transfer = transfer;
            FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
        }

        public CottonQueuedUploadExecutionStatus Status { get; }

        public CottonTransferQueueItem? Transfer { get; }

        public string? FailureMessage { get; }
    }
}
