// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredSyncedFileManifest
    {
        public int SchemaVersion { get; set; }

        public string? SyncRootStableKey { get; set; }

        public DateTime SavedAtUtc { get; set; }

        public List<CottonStoredSyncedFileItem>? Items { get; set; }
    }
}
