// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonRecentFileListSnapshot
    {
        private CottonRecentFileListSnapshot(IReadOnlyList<CottonRecentFileListItem> items)
        {
            Items = items;
            SummaryText = items.Count switch
            {
                0 => "No recent files",
                1 => "1 recent file",
                _ => $"{items.Count} recent files",
            };
        }

        public IReadOnlyList<CottonRecentFileListItem> Items { get; }

        public string SummaryText { get; }

        public string EmptyMessage => "No recent files yet";

        public string EmptyDetails => "Open, download, or share files to build this list.";

        public bool IsEmpty => Items.Count == 0;

        public bool IsListVisible => !IsEmpty;

        public static CottonRecentFileListSnapshot Create(IEnumerable<CottonRecentFileSnapshot> files)
        {
            ArgumentNullException.ThrowIfNull(files);

            List<CottonRecentFileListItem> items = files
                .OrderByDescending(file => file.LastUsedAtUtc)
                .ThenBy(file => file.FileName, StringComparer.OrdinalIgnoreCase)
                .Select(file => new CottonRecentFileListItem(file))
                .ToList();
            return new CottonRecentFileListSnapshot(items);
        }
    }
}
