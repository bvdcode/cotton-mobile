// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestorePreview
    {
        internal CottonMediaOriginalRestorePreview(
            CottonSyncRootSnapshot root,
            IEnumerable<CottonMediaOriginalRestoreItem> items,
            int unchangedCount,
            int unverifiedCount)
        {
            Root = root;
            Items = [.. items];
            UnchangedCount = unchangedCount;
            UnverifiedCount = unverifiedCount;
        }

        public CottonSyncRootSnapshot Root { get; }

        public IReadOnlyList<CottonMediaOriginalRestoreItem> Items { get; }

        public int UnchangedCount { get; }

        public int UnverifiedCount { get; }
    }
}
