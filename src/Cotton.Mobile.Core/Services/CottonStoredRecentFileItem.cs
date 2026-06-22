// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredRecentFileItem
    {
        public Guid FileId { get; set; }

        public string? FileName { get; set; }

        public string? Kind { get; set; }

        public string? BadgeText { get; set; }

        public DateTime RemoteUpdatedAtUtc { get; set; }

        public long? SizeBytes { get; set; }

        public string? ContentType { get; set; }

        public DateTime LastUsedAtUtc { get; set; }

        public CottonRecentFileActionKind LastAction { get; set; }
    }
}
