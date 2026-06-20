namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupActivityIndicator
    {
        private CottonCameraBackupActivityIndicator(
            int activeCount,
            int failedCount,
            int runningCount,
            int queuedCount,
            int pausedCount)
        {
            if (activeCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(activeCount), "Active backup count cannot be negative.");
            }

            if (failedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedCount), "Failed backup count cannot be negative.");
            }

            if (runningCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(runningCount), "Running backup count cannot be negative.");
            }

            if (queuedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queuedCount), "Queued backup count cannot be negative.");
            }

            if (pausedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pausedCount), "Paused backup count cannot be negative.");
            }

            ActiveCount = activeCount;
            FailedCount = failedCount;
            RunningCount = runningCount;
            QueuedCount = queuedCount;
            PausedCount = pausedCount;
            Text = CreateText();
            Details = CreateDetails();
            AccessibilityText = IsVisible
                ? $"{Text}. {Details}. Open camera backup."
                : "No camera backup activity.";
        }

        public static CottonCameraBackupActivityIndicator Empty { get; } = new(0, 0, 0, 0, 0);

        public int ActiveCount { get; }

        public int FailedCount { get; }

        public int RunningCount { get; }

        public int QueuedCount { get; }

        public int PausedCount { get; }

        public bool IsVisible => ActiveCount > 0;

        public bool HasFailures => FailedCount > 0;

        public string Text { get; }

        public string Details { get; }

        public string AccessibilityText { get; }

        public static CottonCameraBackupActivityIndicator Create(IEnumerable<CottonTransferQueueItem> transfers)
        {
            ArgumentNullException.ThrowIfNull(transfers);

            int failedCount = 0;
            int runningCount = 0;
            int queuedCount = 0;
            int pausedCount = 0;

            foreach (CottonTransferQueueItem transfer in transfers)
            {
                if (transfer.Source?.Kind != CottonTransferSourceKind.CameraBackup)
                {
                    continue;
                }

                switch (transfer.Status)
                {
                    case CottonTransferStatus.Failed:
                        failedCount++;
                        break;
                    case CottonTransferStatus.Running:
                        runningCount++;
                        break;
                    case CottonTransferStatus.Queued:
                        queuedCount++;
                        break;
                    case CottonTransferStatus.Paused:
                        pausedCount++;
                        break;
                }
            }

            return new CottonCameraBackupActivityIndicator(
                failedCount + runningCount + queuedCount + pausedCount,
                failedCount,
                runningCount,
                queuedCount,
                pausedCount);
        }

        private string CreateText()
        {
            if (!IsVisible)
            {
                return string.Empty;
            }

            if (FailedCount > 0)
            {
                return FormatCount(FailedCount, "backup failed", "backups failed");
            }

            if (RunningCount > 0)
            {
                return RunningCount == 1 ? "Backing up 1 item" : $"Backing up {RunningCount:N0} items";
            }

            if (PausedCount > 0)
            {
                return FormatCount(PausedCount, "backup paused", "backups paused");
            }

            return FormatCount(QueuedCount, "backup waiting", "backups waiting");
        }

        private string CreateDetails()
        {
            if (!IsVisible)
            {
                return string.Empty;
            }

            if (FailedCount > 0)
            {
                return CreateFailedDetails();
            }

            if (RunningCount > 0 && QueuedCount > 0)
            {
                return $"{FormatCount(RunningCount, "running", "running")}, {FormatCount(QueuedCount, "waiting", "waiting")}";
            }

            if (PausedCount > 0)
            {
                return "Paused";
            }

            return "Tap for backup";
        }

        private static string FormatCount(int count, string singular, string plural)
        {
            return count == 1 ? $"1 {singular}" : $"{count:N0} {plural}";
        }

        private string CreateFailedDetails()
        {
            if (RunningCount > 0)
            {
                return FormatCount(RunningCount, "running", "running");
            }

            if (QueuedCount > 0)
            {
                return FormatCount(QueuedCount, "waiting", "waiting");
            }

            if (PausedCount > 0)
            {
                return FormatCount(PausedCount, "paused", "paused");
            }

            return "Tap to review";
        }
    }
}
