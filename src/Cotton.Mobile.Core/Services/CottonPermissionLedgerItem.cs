// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonPermissionLedgerItem
    {
        public CottonPermissionLedgerItem(
            string title,
            string statusText,
            string detailText,
            bool needsAttention)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(statusText);
            ArgumentException.ThrowIfNullOrWhiteSpace(detailText);

            Title = title.Trim();
            StatusText = statusText.Trim();
            DetailText = detailText.Trim();
            NeedsAttention = needsAttention;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string DetailText { get; }

        public bool NeedsAttention { get; }
    }
}
