// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCachedFolderContentSnapshot
    {
        public CottonCachedFolderContentSnapshot(
            CottonFolderContent content,
            DateTime cachedAtUtc)
        {
            ArgumentNullException.ThrowIfNull(content);

            Content = content;
            CachedAtUtc = CottonLocalFileFreshness.NormalizeUtc(cachedAtUtc);
        }

        public CottonFolderContent Content { get; }

        public DateTime CachedAtUtc { get; }
    }
}
