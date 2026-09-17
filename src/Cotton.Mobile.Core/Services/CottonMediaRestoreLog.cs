// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public static partial class CottonMediaRestoreLog
    {
        [LoggerMessage(EventId = 2301, Level = LogLevel.Information,
            Message = "Original media scan for root {RootId}: {VerifiedCount} restorable, {UnchangedCount} unchanged, {UnverifiedCount} unverified.")]
        public static partial void ScanCompleted(ILogger logger, Guid rootId, int verifiedCount, int unchangedCount, int unverifiedCount);

        [LoggerMessage(EventId = 2302, Level = LogLevel.Information,
            Message = "Restoring original media in root {RootId}, file {FileId}, operation {OperationId}.")]
        public static partial void FileStarted(ILogger logger, Guid rootId, Guid fileId, Guid operationId);

        [LoggerMessage(EventId = 2303, Level = LogLevel.Information,
            Message = "Original media restored in root {RootId}, file {FileId}, operation {OperationId}.")]
        public static partial void FileCompleted(ILogger logger, Guid rootId, Guid fileId, Guid operationId);

        [LoggerMessage(EventId = 2304, Level = LogLevel.Warning,
            Message = "Original media recovery skipped changed file {FileId} in root {RootId}.")]
        public static partial void FileChanged(ILogger logger, Guid rootId, Guid fileId);
    }
}
