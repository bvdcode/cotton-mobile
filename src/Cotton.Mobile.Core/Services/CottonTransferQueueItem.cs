namespace Cotton.Mobile.Services
{
    public class CottonTransferQueueItem
    {
        private CottonTransferQueueItem(
            Guid id,
            CottonTransferKind kind,
            string displayName,
            CottonTransferStatus status,
            CottonTransferProgressSnapshot progress,
            int attemptCount,
            string? failureMessage,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            CottonTransferDestinationSnapshot? destination,
            string? contentType,
            CottonTransferSourceSnapshot? source)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Transfer display name is required.", nameof(displayName));
            }

            if (attemptCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(attemptCount), "Attempt count cannot be negative.");
            }

            if (!Enum.IsDefined(kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "Transfer kind is not supported.");
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Transfer status is not supported.");
            }

            ArgumentNullException.ThrowIfNull(progress);

            Id = id;
            Kind = kind;
            DisplayName = displayName.Trim();
            Status = status;
            Progress = progress;
            AttemptCount = attemptCount;
            FailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
            CreatedAtUtc = NormalizeUtc(createdAtUtc);
            UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
            Destination = destination;
            ContentType = NormalizeContentType(contentType);
            Source = source;
        }

        public Guid Id { get; }

        public CottonTransferKind Kind { get; }

        public string DisplayName { get; }

        public CottonTransferStatus Status { get; }

        public CottonTransferProgressSnapshot Progress { get; }

        public int AttemptCount { get; }

        public string? FailureMessage { get; }

        public DateTime CreatedAtUtc { get; }

        public DateTime UpdatedAtUtc { get; }

        public CottonTransferDestinationSnapshot? Destination { get; }

        public string ContentType { get; }

        public CottonTransferSourceSnapshot? Source { get; }

        public bool IsTerminal => Status is CottonTransferStatus.Completed or CottonTransferStatus.Cancelled;

        public bool CanRetry => Status == CottonTransferStatus.Failed;

        public bool CanCancel => !IsTerminal;

        public static CottonTransferQueueItem CreateUpload(
            Guid id,
            string displayName,
            long? totalBytes,
            DateTime createdAtUtc)
        {
            return CreateUpload(id, displayName, totalBytes, createdAtUtc, destination: null);
        }

        public static CottonTransferQueueItem CreateUpload(
            Guid id,
            string displayName,
            long? totalBytes,
            DateTime createdAtUtc,
            CottonTransferDestinationSnapshot? destination)
        {
            return CreateUpload(
                id,
                displayName,
                totalBytes,
                createdAtUtc,
                destination,
                contentType: null);
        }

        public static CottonTransferQueueItem CreateUpload(
            Guid id,
            string displayName,
            long? totalBytes,
            DateTime createdAtUtc,
            CottonTransferDestinationSnapshot? destination,
            string? contentType)
        {
            return CreateUpload(
                id,
                displayName,
                totalBytes,
                createdAtUtc,
                destination,
                contentType,
                source: null);
        }

        public static CottonTransferQueueItem CreateUpload(
            Guid id,
            string displayName,
            long? totalBytes,
            DateTime createdAtUtc,
            CottonTransferDestinationSnapshot? destination,
            string? contentType,
            CottonTransferSourceSnapshot? source)
        {
            return new CottonTransferQueueItem(
                id,
                CottonTransferKind.Upload,
                displayName,
                CottonTransferStatus.Queued,
                new CottonTransferProgressSnapshot(0, totalBytes),
                0,
                null,
                createdAtUtc,
                createdAtUtc,
                destination,
                contentType,
                source);
        }

        public static CottonTransferQueueItem Restore(
            Guid id,
            CottonTransferKind kind,
            string displayName,
            CottonTransferStatus status,
            long transferredBytes,
            long? totalBytes,
            int attemptCount,
            string? failureMessage,
            DateTime createdAtUtc,
            DateTime updatedAtUtc)
        {
            return Restore(
                id,
                kind,
                displayName,
                status,
                transferredBytes,
                totalBytes,
                attemptCount,
                failureMessage,
                createdAtUtc,
                updatedAtUtc,
                destination: null);
        }

        public static CottonTransferQueueItem Restore(
            Guid id,
            CottonTransferKind kind,
            string displayName,
            CottonTransferStatus status,
            long transferredBytes,
            long? totalBytes,
            int attemptCount,
            string? failureMessage,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            CottonTransferDestinationSnapshot? destination)
        {
            return Restore(
                id,
                kind,
                displayName,
                status,
                transferredBytes,
                totalBytes,
                attemptCount,
                failureMessage,
                createdAtUtc,
                updatedAtUtc,
                destination,
                contentType: null);
        }

        public static CottonTransferQueueItem Restore(
            Guid id,
            CottonTransferKind kind,
            string displayName,
            CottonTransferStatus status,
            long transferredBytes,
            long? totalBytes,
            int attemptCount,
            string? failureMessage,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            CottonTransferDestinationSnapshot? destination,
            string? contentType)
        {
            return Restore(
                id,
                kind,
                displayName,
                status,
                transferredBytes,
                totalBytes,
                attemptCount,
                failureMessage,
                createdAtUtc,
                updatedAtUtc,
                destination,
                contentType,
                source: null);
        }

        public static CottonTransferQueueItem Restore(
            Guid id,
            CottonTransferKind kind,
            string displayName,
            CottonTransferStatus status,
            long transferredBytes,
            long? totalBytes,
            int attemptCount,
            string? failureMessage,
            DateTime createdAtUtc,
            DateTime updatedAtUtc,
            CottonTransferDestinationSnapshot? destination,
            string? contentType,
            CottonTransferSourceSnapshot? source)
        {
            return new CottonTransferQueueItem(
                id,
                kind,
                displayName,
                status,
                new CottonTransferProgressSnapshot(transferredBytes, totalBytes),
                attemptCount,
                failureMessage,
                createdAtUtc,
                updatedAtUtc,
                destination,
                contentType,
                source);
        }

        public CottonTransferQueueItem Start(DateTime updatedAtUtc)
        {
            EnsureStatus(
                [CottonTransferStatus.Queued, CottonTransferStatus.Paused],
                nameof(Start));
            return With(
                CottonTransferStatus.Running,
                Progress,
                AttemptCount + 1,
                null,
                updatedAtUtc);
        }

        public CottonTransferQueueItem ReportProgress(long transferredBytes, DateTime updatedAtUtc)
        {
            EnsureStatus([CottonTransferStatus.Running], nameof(ReportProgress));
            if (transferredBytes < Progress.TransferredBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(transferredBytes),
                    "Transfer progress cannot move backwards.");
            }

            return With(
                CottonTransferStatus.Running,
                new CottonTransferProgressSnapshot(transferredBytes, Progress.TotalBytes),
                AttemptCount,
                null,
                updatedAtUtc);
        }

        public CottonTransferQueueItem Pause(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonTransferStatus.Running], nameof(Pause));
            return With(CottonTransferStatus.Paused, Progress, AttemptCount, null, updatedAtUtc);
        }

        public CottonTransferQueueItem Complete(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonTransferStatus.Running], nameof(Complete));
            CottonTransferProgressSnapshot completedProgress = Progress.TotalBytes.HasValue
                ? new CottonTransferProgressSnapshot(Progress.TotalBytes.Value, Progress.TotalBytes)
                : Progress;
            return With(CottonTransferStatus.Completed, completedProgress, AttemptCount, null, updatedAtUtc);
        }

        public CottonTransferQueueItem Fail(string failureMessage, DateTime updatedAtUtc)
        {
            EnsureStatus([CottonTransferStatus.Running], nameof(Fail));
            return With(CottonTransferStatus.Failed, Progress, AttemptCount, failureMessage, updatedAtUtc);
        }

        public CottonTransferQueueItem MarkFailed(string failureMessage, DateTime updatedAtUtc)
        {
            if (IsTerminal)
            {
                throw new InvalidOperationException($"Cannot mark a {Status} transfer as failed.");
            }

            return With(CottonTransferStatus.Failed, Progress, AttemptCount, failureMessage, updatedAtUtc);
        }

        public CottonTransferQueueItem Retry(DateTime updatedAtUtc)
        {
            EnsureStatus([CottonTransferStatus.Failed], nameof(Retry));
            return With(CottonTransferStatus.Queued, Progress, AttemptCount, null, updatedAtUtc);
        }

        public CottonTransferQueueItem Cancel(DateTime updatedAtUtc)
        {
            if (!CanCancel)
            {
                throw new InvalidOperationException($"Cannot cancel a {Status} transfer.");
            }

            return With(CottonTransferStatus.Cancelled, Progress, AttemptCount, null, updatedAtUtc);
        }

        public CottonTransferQueueItem RestoreAfterRestart(DateTime updatedAtUtc)
        {
            return Status == CottonTransferStatus.Running
                ? With(CottonTransferStatus.Queued, Progress, AttemptCount, null, updatedAtUtc)
                : this;
        }

        private CottonTransferQueueItem With(
            CottonTransferStatus status,
            CottonTransferProgressSnapshot progress,
            int attemptCount,
            string? failureMessage,
            DateTime updatedAtUtc)
        {
            return new CottonTransferQueueItem(
                Id,
                Kind,
                DisplayName,
                status,
                progress,
                attemptCount,
                failureMessage,
                CreatedAtUtc,
                updatedAtUtc,
                Destination,
                ContentType,
                Source);
        }

        private void EnsureStatus(CottonTransferStatus[] allowedStatuses, string operationName)
        {
            if (!allowedStatuses.Contains(Status))
            {
                throw new InvalidOperationException($"Cannot {operationName} a {Status} transfer.");
            }
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        private static string NormalizeContentType(string? contentType)
        {
            return string.IsNullOrWhiteSpace(contentType)
                ? CottonFileUploadSourceSnapshot.DefaultContentType
                : contentType.Trim();
        }
    }
}
