// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonSyncConflictDetectionStatus
    {
        Ready,
        NeedsFreshServerRevision,
        ServerRevisionChanged,
        RemoteTargetMissing,
        RemoteTargetTypeChanged,
        RemoteTargetMismatch,
        FolderRevisionUnsupported,
        RootNotReady,
        WrongSyncRoot,
        TerminalJournalItem,
    }
}
