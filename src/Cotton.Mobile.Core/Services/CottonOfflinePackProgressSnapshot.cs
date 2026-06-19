namespace Cotton.Mobile.Services
{
    public class CottonOfflinePackProgressSnapshot
    {
        public CottonOfflinePackProgressSnapshot(
            CottonOfflinePackProgressStatus status,
            string folderName,
            int completedCount,
            int totalCount,
            long completedBytes,
            long totalBytes,
            string? currentFileName = null,
            string? failureText = null)
        {
            Validate(status, folderName, completedCount, totalCount, completedBytes, totalBytes);

            Status = status;
            FolderName = string.IsNullOrWhiteSpace(folderName) ? string.Empty : folderName.Trim();
            CompletedCount = completedCount;
            TotalCount = totalCount;
            CompletedBytes = completedBytes;
            TotalBytes = totalBytes;
            CurrentFileName = string.IsNullOrWhiteSpace(currentFileName) ? null : currentFileName.Trim();
            FailureText = string.IsNullOrWhiteSpace(failureText) ? null : failureText.Trim();
            Text = CreateText();
            Details = CreateDetails();
            AccessibilityText = IsVisible
                ? $"{Text}. {Details}."
                : "No offline folder activity.";
        }

        public static CottonOfflinePackProgressSnapshot Empty { get; } = new(
            CottonOfflinePackProgressStatus.None,
            string.Empty,
            0,
            0,
            0,
            0);

        public CottonOfflinePackProgressStatus Status { get; }

        public string FolderName { get; }

        public int CompletedCount { get; }

        public int TotalCount { get; }

        public long CompletedBytes { get; }

        public long TotalBytes { get; }

        public string? CurrentFileName { get; }

        public string? FailureText { get; }

        public bool IsVisible => Status != CottonOfflinePackProgressStatus.None;

        public bool IsRunning => Status == CottonOfflinePackProgressStatus.Running;

        public double ProgressFraction => TotalCount == 0 ? 0 : (double)CompletedCount / TotalCount;

        public string Text { get; }

        public string Details { get; }

        public string AccessibilityText { get; }

        public static CottonOfflinePackProgressSnapshot CreateRunning(
            CottonOfflineDownloadQueueSnapshot queue,
            int completedCount,
            long completedBytes,
            CottonOfflineDownloadQueueItem? currentItem = null)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return new CottonOfflinePackProgressSnapshot(
                CottonOfflinePackProgressStatus.Running,
                queue.FolderName,
                completedCount,
                queue.TotalCount,
                completedBytes,
                queue.TotalSizeBytes,
                currentItem?.FileName);
        }

        public static CottonOfflinePackProgressSnapshot CreateCompleted(CottonOfflineDownloadQueueSnapshot queue)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return new CottonOfflinePackProgressSnapshot(
                CottonOfflinePackProgressStatus.Completed,
                queue.FolderName,
                queue.TotalCount,
                queue.TotalCount,
                queue.TotalSizeBytes,
                queue.TotalSizeBytes);
        }

        public static CottonOfflinePackProgressSnapshot CreateCancelled(
            CottonOfflineDownloadQueueSnapshot queue,
            int completedCount,
            long completedBytes)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return new CottonOfflinePackProgressSnapshot(
                CottonOfflinePackProgressStatus.Cancelled,
                queue.FolderName,
                completedCount,
                queue.TotalCount,
                completedBytes,
                queue.TotalSizeBytes);
        }

        public static CottonOfflinePackProgressSnapshot CreateFailed(
            CottonOfflineDownloadQueueSnapshot queue,
            int completedCount,
            long completedBytes,
            string? failureText = null)
        {
            ArgumentNullException.ThrowIfNull(queue);

            return new CottonOfflinePackProgressSnapshot(
                CottonOfflinePackProgressStatus.Failed,
                queue.FolderName,
                completedCount,
                queue.TotalCount,
                completedBytes,
                queue.TotalSizeBytes,
                failureText: failureText);
        }

        private static void Validate(
            CottonOfflinePackProgressStatus status,
            string folderName,
            int completedCount,
            int totalCount,
            long completedBytes,
            long totalBytes)
        {
            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Offline pack progress status is unknown.");
            }

            if (completedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Completed count cannot be negative.");
            }

            if (totalCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), "Total count cannot be negative.");
            }

            if (completedCount > totalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Completed count cannot exceed total count.");
            }

            if (completedBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(completedBytes), "Completed bytes cannot be negative.");
            }

            if (totalBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalBytes), "Total bytes cannot be negative.");
            }

            if (completedBytes > totalBytes)
            {
                throw new ArgumentOutOfRangeException(nameof(completedBytes), "Completed bytes cannot exceed total bytes.");
            }

            if (status == CottonOfflinePackProgressStatus.None)
            {
                if (!string.IsNullOrWhiteSpace(folderName) || totalCount != 0 || completedCount != 0 || totalBytes != 0 || completedBytes != 0)
                {
                    throw new ArgumentException("Empty offline pack progress cannot include folder or progress values.", nameof(status));
                }

                return;
            }

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new ArgumentException("Folder name is required.", nameof(folderName));
            }

            if (totalCount == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalCount), "Visible offline pack progress requires files.");
            }

            if (status == CottonOfflinePackProgressStatus.Completed && completedCount != totalCount)
            {
                throw new ArgumentOutOfRangeException(nameof(completedCount), "Completed offline pack progress requires every file.");
            }
        }

        private string CreateText()
        {
            return Status switch
            {
                CottonOfflinePackProgressStatus.Running => $"Keeping {FolderName} offline",
                CottonOfflinePackProgressStatus.Completed => $"{FolderName} available offline",
                CottonOfflinePackProgressStatus.Cancelled => $"{FolderName} offline cancelled",
                CottonOfflinePackProgressStatus.Failed => $"{FolderName} offline failed",
                _ => string.Empty,
            };
        }

        private string CreateDetails()
        {
            return Status switch
            {
                CottonOfflinePackProgressStatus.Running => CreateRunningDetails(),
                CottonOfflinePackProgressStatus.Completed => $"{FormatFileCount(TotalCount)} · {CottonFileSizeFormatter.Format(TotalBytes)}",
                CottonOfflinePackProgressStatus.Cancelled => $"{FormatProgress()} saved",
                CottonOfflinePackProgressStatus.Failed => CreateFailedDetails(),
                _ => string.Empty,
            };
        }

        private string CreateRunningDetails()
        {
            string details = $"{FormatProgress()} · {CottonFileSizeFormatter.Format(CompletedBytes)} of {CottonFileSizeFormatter.Format(TotalBytes)}";
            return string.IsNullOrWhiteSpace(CurrentFileName)
                ? details
                : $"{details} · Saving {CurrentFileName}";
        }

        private string CreateFailedDetails()
        {
            string details = $"{FormatProgress()} saved";
            return string.IsNullOrWhiteSpace(FailureText)
                ? details
                : $"{details} · {FailureText}";
        }

        private string FormatProgress()
        {
            return $"{CompletedCount}/{TotalCount} files";
        }

        private static string FormatFileCount(int count)
        {
            return count == 1 ? "1 file" : $"{count} files";
        }
    }
}
