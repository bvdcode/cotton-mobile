// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileTileLayoutPlanner
    {
        private const double SlotHorizontalPadding = 2;
        private const double SlotRoundingGuard = 1;
        private const int PreferredColumnCount = 2;
        private const double MinimumColumnWidth = 140;
        private const double PreviewRatio = 0.62;
        private const double FolderIconMinimumSize = 62;
        private const double FolderIconMaximumSize = 92;
        private const double FolderIconWidthRatio = 0.42;
        private const double VerticalChrome = 68;

        public static CottonFileTileLayoutMetrics InitialMetrics { get; } = new(
            slotWidth: 150,
            previewHeight: 72,
            folderIconSize: FolderIconMinimumSize,
            tileHeight: 146);

        public static CottonFileTileLayoutMetrics Calculate(double contentWidth)
        {
            if (contentWidth <= 0 || double.IsNaN(contentWidth) || double.IsInfinity(contentWidth))
            {
                throw new ArgumentOutOfRangeException(nameof(contentWidth));
            }

            double slotWidth = ResolveSlotWidth(contentWidth);
            double tileWidth = slotWidth - SlotHorizontalPadding;
            double previewHeight = Math.Round(tileWidth * PreviewRatio);
            double folderIconSize = Math.Clamp(
                Math.Round(tileWidth * FolderIconWidthRatio),
                FolderIconMinimumSize,
                FolderIconMaximumSize);

            return new CottonFileTileLayoutMetrics(
                slotWidth,
                previewHeight,
                folderIconSize,
                previewHeight + VerticalChrome);
        }

        private static double ResolveSlotWidth(double contentWidth)
        {
            int columnCount = contentWidth >= MinimumColumnWidth * PreferredColumnCount
                ? PreferredColumnCount
                : 1;

            return Math.Max(
                1,
                Math.Floor(contentWidth / columnCount) - SlotRoundingGuard);
        }
    }
}
