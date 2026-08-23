// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonAutomaticSyncFailure
    {
        public CottonAutomaticSyncFailure(Guid rootId, CottonAutomaticSyncFailureKind kind)
        {
            if (rootId == Guid.Empty)
            {
                throw new ArgumentException("Sync root id is required.", nameof(rootId));
            }

            if (!Enum.IsDefined(kind) || kind == CottonAutomaticSyncFailureKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), "A sync failure kind is required.");
            }

            RootId = rootId;
            Kind = kind;
        }

        public Guid RootId { get; }

        public CottonAutomaticSyncFailureKind Kind { get; }
    }
}
