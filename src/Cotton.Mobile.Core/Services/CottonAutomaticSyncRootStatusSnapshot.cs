// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRootStatusSnapshot
    {
        public CottonAutomaticSyncRootStatusSnapshot(
            Guid rootId,
            CottonAutomaticSyncOutcome outcome,
            DateTime completedAtUtc,
            CottonAutomaticSyncFailureKind failureKind)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (!Enum.IsDefined(outcome))
            {
                throw new ArgumentOutOfRangeException(nameof(outcome), "Automatic sync outcome is not supported.");
            }

            if (completedAtUtc.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Automatic sync completion time must be UTC.", nameof(completedAtUtc));
            }

            if (!Enum.IsDefined(failureKind))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(failureKind),
                    "Automatic sync failure kind is not supported.");
            }

            bool isValidFailure = outcome == CottonAutomaticSyncOutcome.Failed
                && failureKind != CottonAutomaticSyncFailureKind.None;
            bool isValidSuccess = outcome == CottonAutomaticSyncOutcome.Succeeded
                && failureKind == CottonAutomaticSyncFailureKind.None;
            if (!isValidFailure && !isValidSuccess)
            {
                throw new ArgumentException(
                    "Automatic sync outcome and failure kind do not agree.",
                    nameof(failureKind));
            }

            RootId = rootId;
            Outcome = outcome;
            CompletedAtUtc = completedAtUtc;
            FailureKind = failureKind;
        }

        public Guid RootId { get; }

        public CottonAutomaticSyncOutcome Outcome { get; }

        public DateTime CompletedAtUtc { get; }

        public CottonAutomaticSyncFailureKind FailureKind { get; }

        public static CottonAutomaticSyncRootStatusSnapshot Succeeded(
            Guid rootId,
            DateTime completedAtUtc)
        {
            return new CottonAutomaticSyncRootStatusSnapshot(
                rootId,
                CottonAutomaticSyncOutcome.Succeeded,
                completedAtUtc,
                CottonAutomaticSyncFailureKind.None);
        }

        public static CottonAutomaticSyncRootStatusSnapshot Failed(
            Guid rootId,
            DateTime completedAtUtc,
            CottonAutomaticSyncFailureKind failureKind)
        {
            return new CottonAutomaticSyncRootStatusSnapshot(
                rootId,
                CottonAutomaticSyncOutcome.Failed,
                completedAtUtc,
                failureKind);
        }
    }
}
