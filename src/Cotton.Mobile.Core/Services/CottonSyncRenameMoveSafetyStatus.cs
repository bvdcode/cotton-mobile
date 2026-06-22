// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonSyncRenameMoveSafetyStatus
    {
        ConflictSafe,
        NeedsFreshFileETag,
        FolderRevisionUnsupported,
        InvalidName,
        DuplicateName,
        InvalidMoveTarget,
        SelfMoveUnsupported,
        NoChange,
    }
}
