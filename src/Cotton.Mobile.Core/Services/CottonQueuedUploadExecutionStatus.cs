// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public enum CottonQueuedUploadExecutionStatus
    {
        NoQueuedUpload = 0,
        MissingDestination = 1,
        MissingStagedFile = 2,
        Completed = 3,
        Failed = 4,
        TransferNotFound = 5,
        TransferNotQueued = 6,
    }
}
