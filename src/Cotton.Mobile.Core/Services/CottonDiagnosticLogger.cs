// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    internal class CottonDiagnosticLogger(
        ICottonDiagnosticJournal journal,
        string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return CottonDiagnosticCategoryPolicy.IsEnabled(category, logLevel);
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
            {
                return;
            }

            try
            {
                journal.Write(
                    logLevel,
                    category,
                    eventId,
                    formatter(state, exception),
                    exception?.GetType());
            }
            catch (Exception writeException) when (writeException is IOException
                or UnauthorizedAccessException
                or ObjectDisposedException)
            {
                Trace.TraceError(
                    "Cotton diagnostic journal write failed with {0}.",
                    writeException.GetType().FullName);
            }
        }
    }
}
