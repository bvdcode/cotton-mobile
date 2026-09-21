// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncReviewState(
        string syncRootStableKey,
        string? baselineFingerprint,
        int unchangedCount,
        IReadOnlyList<CottonSyncConflictSnapshot> conflicts,
        IReadOnlyList<CottonSyncReplacementApproval> approvals,
        IReadOnlyList<CottonSyncVerifiedFile> verifiedFiles)
    {
        public string SyncRootStableKey { get; } = syncRootStableKey;
        public string? BaselineFingerprint { get; } = baselineFingerprint;
        public int UnchangedCount { get; } = unchangedCount;
        public IReadOnlyList<CottonSyncConflictSnapshot> Conflicts { get; } = conflicts;
        public IReadOnlyList<CottonSyncReplacementApproval> Approvals { get; } = approvals;
        public IReadOnlyList<CottonSyncVerifiedFile> VerifiedFiles { get; } = verifiedFiles;
    }
}
