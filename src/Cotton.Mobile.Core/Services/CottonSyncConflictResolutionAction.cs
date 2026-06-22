// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonSyncConflictResolutionAction
    {
        Refresh,
        KeepLocalChange,
        UseCloudVersion,
        SkipLocalChange,
        ReconnectLocalRoot,
        Dismiss,
    }
}
