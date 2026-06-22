// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonLocalNotificationKind
    {
        TransferCompleted = 0,
        TransferFailed = 1,
        BackupBlocked = 2,
        RemoteSharedFile = 3,
        RemoteAccessRequest = 4,
        RemoteCommentMention = 5,
        RemoteSecuritySession = 6,
    }
}
