// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonMediaOriginalRestoreState(
        string syncRootStableKey,
        bool reviewCompleted,
        IReadOnlyList<CottonMediaOriginalRestoreApproval> approvals)
    {
        public string SyncRootStableKey { get; } = syncRootStableKey;

        public bool ReviewCompleted { get; } = reviewCompleted;

        public IReadOnlyList<CottonMediaOriginalRestoreApproval> Approvals { get; } = [.. approvals];
    }
}
