// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class PdfPreviewDocumentSnapshot
    {
        public PdfPreviewDocumentSnapshot(
            int totalPageCount,
            IReadOnlyList<PdfPreviewPageSnapshot> pages)
        {
            ArgumentNullException.ThrowIfNull(pages);
            if (totalPageCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalPageCount));
            }

            TotalPageCount = totalPageCount;
            Pages = pages;
        }

        public int TotalPageCount { get; }

        public IReadOnlyList<PdfPreviewPageSnapshot> Pages { get; }

        public bool HasPages => Pages.Count > 0;

        public string StatusText
        {
            get
            {
                if (Pages.Count > 0 && Pages.Count < TotalPageCount)
                {
                    return $"Showing first {Pages.Count} of {TotalPageCount} pages";
                }

                return TotalPageCount == 1
                    ? "1 page"
                    : $"{TotalPageCount} pages";
            }
        }
    }
}
