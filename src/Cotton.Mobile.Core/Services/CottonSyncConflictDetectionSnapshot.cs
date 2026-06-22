// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncConflictDetectionSnapshot
    {
        public CottonSyncConflictDetectionSnapshot(
            CottonSyncJournalOperation operation,
            CottonSyncConflictDetectionStatus status,
            string? expectedETag,
            string? actualETag,
            string summaryText)
        {
            if (!Enum.IsDefined(operation))
            {
                throw new ArgumentOutOfRangeException(nameof(operation), "Sync journal operation is not supported.");
            }

            if (!Enum.IsDefined(status))
            {
                throw new ArgumentOutOfRangeException(nameof(status), "Sync conflict status is not supported.");
            }

            Operation = operation;
            Status = status;
            ExpectedETag = string.IsNullOrWhiteSpace(expectedETag) ? null : expectedETag.Trim();
            ActualETag = string.IsNullOrWhiteSpace(actualETag) ? null : actualETag.Trim();
            SummaryText = string.IsNullOrWhiteSpace(summaryText)
                ? throw new ArgumentException("Summary text is required.", nameof(summaryText))
                : summaryText;
        }

        public CottonSyncJournalOperation Operation { get; }

        public CottonSyncConflictDetectionStatus Status { get; }

        public string? ExpectedETag { get; }

        public string? ActualETag { get; }

        public string SummaryText { get; }

        public bool CanExecuteServerMutation => Status == CottonSyncConflictDetectionStatus.Ready;

        public bool CanCompleteWithoutServerMutation =>
            Operation == CottonSyncJournalOperation.Delete
            && Status == CottonSyncConflictDetectionStatus.RemoteTargetMissing;

        public bool NeedsFreshListing => Status == CottonSyncConflictDetectionStatus.NeedsFreshServerRevision;

        public bool RequiresConflictResolution =>
            Status is CottonSyncConflictDetectionStatus.ServerRevisionChanged
                or CottonSyncConflictDetectionStatus.RemoteTargetTypeChanged
                or CottonSyncConflictDetectionStatus.RemoteTargetMismatch
            || (Status == CottonSyncConflictDetectionStatus.RemoteTargetMissing && !CanCompleteWithoutServerMutation);

        public bool IsBlocked => !CanExecuteServerMutation && !CanCompleteWithoutServerMutation;
    }
}
