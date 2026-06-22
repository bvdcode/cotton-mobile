// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonOfflineFolderFreeSpaceWarningKind
    {
        None = 0,
        LargeFolder = 1,
        LowFreeSpaceAfterDownload = 2,
        NotEnoughFreeSpace = 3,
        UnknownFreeSpaceForLargeFolder = 4,
    }
}
