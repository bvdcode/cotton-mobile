// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    internal class CottonStoredContentRevision
    {
        public string? LocalSourceId { get; set; }

        public long Generation { get; set; }

        public string? ContentHash { get; set; }
    }
}
