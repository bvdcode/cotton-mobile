// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredContentRevisionIndex
    {
        public int SchemaVersion { get; set; }

        public string? SyncRootStableKey { get; set; }

        public string? SourceVersion { get; set; }

        public List<CottonStoredContentRevision?>? Revisions { get; set; }
    }
}
