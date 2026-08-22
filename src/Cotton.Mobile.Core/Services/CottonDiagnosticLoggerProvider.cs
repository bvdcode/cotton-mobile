// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    public class CottonDiagnosticLoggerProvider(ICottonDiagnosticJournal journal) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new CottonDiagnosticLogger(journal, categoryName);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
