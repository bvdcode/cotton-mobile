// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using Cotton.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.DependencyInjection
{
    public static class CottonDiagnosticsBuilderExtensions
    {
        public static MauiAppBuilder AddCottonDiagnostics(this MauiAppBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ICottonDiagnosticJournal journal = new FileSystemCottonDiagnosticJournal(
                CottonDiagnosticJournalPathProvider.CreateDirectoryPath(),
                TimeProvider.System);
            builder.Services.AddSingleton(journal);
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
            builder.Logging.AddProvider(new CottonDiagnosticLoggerProvider(journal));
            return builder;
        }
    }
}
