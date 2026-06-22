// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonBidirectionalSyncRunOptions
    {
        private CottonBidirectionalSyncRunOptions(bool allowDestructiveChanges)
        {
            AllowDestructiveChanges = allowDestructiveChanges;
        }

        public bool AllowDestructiveChanges { get; }

        public static CottonBidirectionalSyncRunOptions Default { get; } = new(allowDestructiveChanges: false);

        public static CottonBidirectionalSyncRunOptions AllowDestructiveDeletes { get; } =
            new(allowDestructiveChanges: true);
    }
}
