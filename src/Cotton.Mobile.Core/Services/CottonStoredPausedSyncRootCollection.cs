// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredPausedSyncRootCollection
    {
        public int SchemaVersion { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public List<Guid>? RootIds { get; set; }
    }
}
