// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class PdfPreviewPageSnapshot
    {
        public PdfPreviewPageSnapshot(
            int pageNumber,
            int widthPixels,
            int heightPixels,
            ImageSource imageSource)
        {
            if (pageNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber));
            }

            if (widthPixels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(widthPixels));
            }

            if (heightPixels <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(heightPixels));
            }

            ArgumentNullException.ThrowIfNull(imageSource);

            PageNumber = pageNumber;
            WidthPixels = widthPixels;
            HeightPixels = heightPixels;
            ImageSource = imageSource;
            DisplayHeight = CreateDisplayHeight(widthPixels, heightPixels);
            PageLabel = $"Page {pageNumber}";
        }

        public int PageNumber { get; }

        public int WidthPixels { get; }

        public int HeightPixels { get; }

        public ImageSource ImageSource { get; }

        public double DisplayHeight { get; }

        public string PageLabel { get; }

        private static double CreateDisplayHeight(int widthPixels, int heightPixels)
        {
            double aspectHeight = 360d * heightPixels / widthPixels;
            return Math.Clamp(aspectHeight, 420d, 760d);
        }
    }
}
