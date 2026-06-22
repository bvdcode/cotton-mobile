// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonRecursiveOfflineFolderPlanStatus
    {
        Ready = 0,
        Empty = 1,
        NeedsFolderScan = 2,
        HasUnknownSize = 3,
    }
}
