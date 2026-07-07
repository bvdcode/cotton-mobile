// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileTileLayoutMetrics
    {
        public CottonFileTileLayoutMetrics(
            double slotWidth,
            double previewHeight,
            double folderIconSize,
            double tileHeight,
            int columnCount)
        {
            SlotWidth = slotWidth;
            PreviewHeight = previewHeight;
            FolderIconSize = folderIconSize;
            TileHeight = tileHeight;
            ColumnCount = columnCount;
        }

        public double SlotWidth { get; }

        public double PreviewHeight { get; }

        public double FolderIconSize { get; }

        public double TileHeight { get; }

        public int ColumnCount { get; }
    }
}
