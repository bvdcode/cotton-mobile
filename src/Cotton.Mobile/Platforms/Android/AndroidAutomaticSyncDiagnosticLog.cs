// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Platforms.Android
{
    public static partial class AndroidAutomaticSyncDiagnosticLog
    {
        [LoggerMessage(EventId = 2201, Level = LogLevel.Information, Message = "Android background sync started with trigger {Trigger}; retry root: {HasRetryRoot}.")]
        public static partial void Started(
            ILogger logger,
            CottonAutomaticSyncTrigger trigger,
            bool hasRetryRoot);

        [LoggerMessage(EventId = 2202, Level = LogLevel.Information, Message = "Android background sync found no remembered session.")]
        public static partial void SessionMissing(ILogger logger);

        [LoggerMessage(EventId = 2203, Level = LogLevel.Information, Message = "Android background sync dispatch completed with {FailureCount} failures.")]
        public static partial void DispatchCompleted(ILogger logger, int failureCount);

        [LoggerMessage(EventId = 2204, Level = LogLevel.Information, Message = "Android background sync scheduled {RetryCount} root retries.")]
        public static partial void RetriesScheduled(ILogger logger, int retryCount);

        [LoggerMessage(EventId = 2205, Level = LogLevel.Warning, Message = "Android stopped background work: reason {StopReason}, attempt {RunAttemptCount}.")]
        public static partial void WorkerStopped(ILogger logger, int stopReason, int runAttemptCount);

        [LoggerMessage(EventId = 2206, Level = LogLevel.Warning, Message = "Android stopped MediaStore sync: reason {StopReason}.")]
        public static partial void MediaStoreStopped(ILogger logger, int stopReason);
    }
}
#endif
