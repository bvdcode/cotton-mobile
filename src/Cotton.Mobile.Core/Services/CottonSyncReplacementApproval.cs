// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonSyncReplacementApproval(CottonSyncConflictSnapshot conflict, Guid operationId)
    {
        public CottonSyncConflictSnapshot Conflict { get; } = conflict;
        public Guid OperationId { get; } = operationId;
    }
}
