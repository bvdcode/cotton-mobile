// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Files;

namespace Cotton.Mobile.Services
{
    public static class CottonSyncRestorePolicy
    {
        public static RestoreItemRequestDto CreateDefaultRequest()
        {
            return new RestoreItemRequestDto
            {
                CreateMissingParents = false,
                Overwrite = false,
            };
        }

        public static CottonSyncRestoreOutcomeSnapshot CreateOutcome(RestoreOutcomeDto outcome)
        {
            ArgumentNullException.ThrowIfNull(outcome);

            return outcome.Status switch
            {
                RestoreStatus.Restored => new CottonSyncRestoreOutcomeSnapshot(
                    CottonSyncRestoreOutcomeStatus.Restored,
                    canRetryWithCreateMissingParents: false,
                    canRetryWithOverwrite: false,
                    "Restored"),
                RestoreStatus.ParentMissing => new CottonSyncRestoreOutcomeSnapshot(
                    CottonSyncRestoreOutcomeStatus.ParentMissingNeedsChoice,
                    canRetryWithCreateMissingParents: true,
                    canRetryWithOverwrite: false,
                    "Original parent is missing"),
                RestoreStatus.Conflict => new CottonSyncRestoreOutcomeSnapshot(
                    CottonSyncRestoreOutcomeStatus.ConflictNeedsChoice,
                    canRetryWithCreateMissingParents: false,
                    canRetryWithOverwrite: true,
                    "Restore conflict"),
                RestoreStatus.NotRestorable => new CottonSyncRestoreOutcomeSnapshot(
                    CottonSyncRestoreOutcomeStatus.NotRestorable,
                    canRetryWithCreateMissingParents: false,
                    canRetryWithOverwrite: false,
                    "Not restorable"),
                _ => throw new ArgumentOutOfRangeException(nameof(outcome), "Restore status is not supported."),
            };
        }
    }
}
