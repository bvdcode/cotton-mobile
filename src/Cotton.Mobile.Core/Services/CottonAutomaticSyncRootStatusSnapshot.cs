// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncRootStatusSnapshot
    {
        public CottonAutomaticSyncRootStatusSnapshot(
            Guid rootId,
            CottonAutomaticSyncOutcome outcome,
            DateTime completedAtUtc)
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

            RootId = rootId;
            Outcome = outcome;
            CompletedAtUtc = completedAtUtc;
        }

        public Guid RootId { get; }

        public CottonAutomaticSyncOutcome Outcome { get; }

        public DateTime CompletedAtUtc { get; }
    }
}
