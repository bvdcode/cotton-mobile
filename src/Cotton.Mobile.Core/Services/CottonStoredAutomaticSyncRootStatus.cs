// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredAutomaticSyncRootStatus
    {
        public Guid RootId { get; set; }

        public CottonAutomaticSyncOutcome Outcome { get; set; }

        public CottonAutomaticSyncFailureKind FailureKind { get; set; }

        public DateTime CompletedAtUtc { get; set; }
    }
}
