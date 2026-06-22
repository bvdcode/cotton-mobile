// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncDeleteSemanticsSnapshot
    {
        public CottonSyncDeleteSemanticsSnapshot(
            CottonFileBrowserEntryType targetType,
            CottonSyncDeleteMode mode,
            CottonSyncDeleteSafetyStatus safetyStatus,
            string? expectedETag,
            string summaryText)
        {
            if (!Enum.IsDefined(targetType))
            {
                throw new ArgumentOutOfRangeException(nameof(targetType), "Delete target type is not supported.");
            }

            if (!Enum.IsDefined(mode))
            {
                throw new ArgumentOutOfRangeException(nameof(mode), "Delete mode is not supported.");
            }

            if (!Enum.IsDefined(safetyStatus))
            {
                throw new ArgumentOutOfRangeException(nameof(safetyStatus), "Delete safety status is not supported.");
            }

            TargetType = targetType;
            Mode = mode;
            SafetyStatus = safetyStatus;
            ExpectedETag = string.IsNullOrWhiteSpace(expectedETag) ? null : expectedETag.Trim();
            SummaryText = string.IsNullOrWhiteSpace(summaryText)
                ? throw new ArgumentException("Summary text is required.", nameof(summaryText))
                : summaryText;
        }

        public CottonFileBrowserEntryType TargetType { get; }

        public CottonSyncDeleteMode Mode { get; }

        public CottonSyncDeleteSafetyStatus SafetyStatus { get; }

        public string? ExpectedETag { get; }

        public string SummaryText { get; }

        public bool UsesServerTrash => Mode == CottonSyncDeleteMode.MoveToTrash;

        public bool SkipsServerTrash => Mode == CottonSyncDeleteMode.Permanent;

        public bool RequiresExplicitConfirmation => Mode == CottonSyncDeleteMode.Permanent;

        public bool HasConflictPrecondition => SafetyStatus == CottonSyncDeleteSafetyStatus.ConflictSafe;

        public bool RequiresExpectedETag => TargetType == CottonFileBrowserEntryType.File;
    }
}
