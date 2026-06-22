// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCachedFolderContent
    {
        public int SchemaVersion { get; set; }

        public Guid FolderId { get; set; }

        public string FolderName { get; set; } = string.Empty;

        public DateTime CachedAtUtc { get; set; }

        public List<CottonCachedFileBrowserEntry> Entries { get; set; } = [];
    }
}
