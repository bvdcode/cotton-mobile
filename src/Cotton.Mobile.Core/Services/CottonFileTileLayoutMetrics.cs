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
            double tileHeight)
        {
            SlotWidth = slotWidth;
            PreviewHeight = previewHeight;
            FolderIconSize = folderIconSize;
            TileHeight = tileHeight;
        }

        public double SlotWidth { get; }

        public double PreviewHeight { get; }

        public double FolderIconSize { get; }

        public double TileHeight { get; }
    }
}
