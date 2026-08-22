// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public static partial class CottonSessionDiagnosticLog
    {
        [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Session restore started.")]
        public static partial void RestoreStarted(ILogger logger);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Information, Message = "Session restore found no saved instance.")]
        public static partial void SavedInstanceMissing(ILogger logger);

        [LoggerMessage(EventId = 2003, Level = LogLevel.Information, Message = "Session restore found no token pair.")]
        public static partial void TokenPairMissing(ILogger logger);

        [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Session token refresh started.")]
        public static partial void RefreshStarted(ILogger logger);

        [LoggerMessage(EventId = 2005, Level = LogLevel.Information, Message = "Session token refresh completed.")]
        public static partial void RefreshCompleted(ILogger logger);

        [LoggerMessage(EventId = 2006, Level = LogLevel.Information, Message = "Session profile validation completed.")]
        public static partial void ProfileValidated(ILogger logger);

        [LoggerMessage(EventId = 2007, Level = LogLevel.Warning, Message = "Session restore was rejected with status {StatusCode}.")]
        public static partial void RestoreRejected(ILogger logger, int statusCode);

        [LoggerMessage(EventId = 2008, Level = LogLevel.Information, Message = "Token store is empty.")]
        public static partial void TokenStoreEmpty(ILogger logger);

        [LoggerMessage(EventId = 2009, Level = LogLevel.Information, Message = "Token store returned a complete pair.")]
        public static partial void TokenStoreLoaded(ILogger logger);

        [LoggerMessage(EventId = 2010, Level = LogLevel.Warning, Message = "Token store contained an incomplete pair and was cleared.")]
        public static partial void TokenStoreIncomplete(ILogger logger);

        [LoggerMessage(EventId = 2011, Level = LogLevel.Warning, Message = "Token store read failed; stored values were preserved.")]
        public static partial void TokenStoreReadFailed(ILogger logger, Exception exception);

        [LoggerMessage(EventId = 2012, Level = LogLevel.Information, Message = "Token store saved a complete pair.")]
        public static partial void TokenStoreSaved(ILogger logger);

        [LoggerMessage(EventId = 2013, Level = LogLevel.Information, Message = "Token store was cleared.")]
        public static partial void TokenStoreCleared(ILogger logger);
    }
}
