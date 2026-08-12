// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    internal static partial class CottonNotificationSessionLog
    {
        [LoggerMessage(EventId = 1001, Level = LogLevel.Warning, Message = "Failed to resume Cotton notification delivery.")]
        public static partial void ResumeFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Warning, Message = "Failed to start Cotton realtime notifications.")]
        public static partial void RealtimeStartFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Warning, Message = "Failed to fetch Cotton notifications.")]
        public static partial void NotificationFetchFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "Failed to request Cotton notification permission.")]
        public static partial void PermissionRequestFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Failed to schedule Cotton background notification polling.")]
        public static partial void BackgroundScheduleFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "Failed to cancel Cotton background notification polling.")]
        public static partial void BackgroundCancelFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 1007, Level = LogLevel.Warning, Message = "Failed to stop Cotton realtime notifications.")]
        public static partial void RealtimeStopFailed(ILogger logger, Exception exception);
    }
}
