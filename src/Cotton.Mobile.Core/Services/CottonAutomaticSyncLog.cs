// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public static partial class CottonAutomaticSyncLog
    {
        [LoggerMessage(EventId = 1101, Level = LogLevel.Warning, Message = "Automatic sync failed for root {RootId}.")]
        public static partial void RootFailed(ILogger logger, Guid rootId, Exception exception);

        [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "Automatic sync resume failed.")]
        public static partial void ResumeFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1103, Level = LogLevel.Warning, Message = "Automatic sync run failed.")]
        public static partial void RunFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1104, Level = LogLevel.Warning, Message = "Automatic sync background scheduling failed.")]
        public static partial void BackgroundScheduleFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1105, Level = LogLevel.Warning, Message = "Automatic sync background cancellation failed.")]
        public static partial void BackgroundCancelFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1106, Level = LogLevel.Debug, Message = "Automatic sync run was canceled.")]
        public static partial void RunCanceled(ILogger logger, Exception exception);
    }
}
