// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public interface ICottonDiagnosticJournal
    {
        void Write(
            LogLevel level,
            string category,
            EventId eventId,
            string message,
            Type? exceptionType);

        IReadOnlyList<string> ReadAll();
    }
}
