// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace Cotton.Mobile.Services
{
    public static partial class CottonLog
    {
        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "{Message}")]
        public static partial void Debug(
            ILogger logger,
            string message,
            Exception? exception = null);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Message}")]
        public static partial void Information(
            ILogger logger,
            string message,
            Exception? exception = null);

        [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "{Message}")]
        public static partial void Warning(
            ILogger logger,
            string message,
            Exception? exception = null);

        [LoggerMessage(EventId = 4, Level = LogLevel.Error, Message = "{Message}")]
        public static partial void Error(
            ILogger logger,
            string message,
            Exception? exception = null);

        [LoggerMessage(EventId = 5, Level = LogLevel.Debug, Message = "{Message} Context: {Context}")]
        public static partial void DebugWithContext(
            ILogger logger,
            string message,
            object? context,
            Exception? exception = null);

        [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "{Message} Context: {Context}")]
        public static partial void WarningWithContext(
            ILogger logger,
            string message,
            object? context,
            Exception? exception = null);

        [LoggerMessage(
            EventId = 10,
            Level = LogLevel.Information,
            Message = "Cotton notification permission result. Context: {Status}")]
        public static partial void NotificationPermissionResult(
            ILogger logger,
            PermissionStatus status);

        [LoggerMessage(
            EventId = 11,
            Level = LogLevel.Debug,
            Message = "{Message} Context: {Context}; file id: {FileId}")]
        public static partial void DebugWithFileId(
            ILogger logger,
            string message,
            string context,
            Guid fileId,
            Exception? exception = null);
    }
}
