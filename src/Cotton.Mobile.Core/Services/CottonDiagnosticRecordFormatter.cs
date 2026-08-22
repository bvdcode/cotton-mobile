// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Cotton.Mobile.Services
{
    internal static class CottonDiagnosticRecordFormatter
    {
        private const int MaximumMessageLength = 2048;

        public static string Format(
            DateTimeOffset timestamp,
            LogLevel level,
            string category,
            EventId eventId,
            string message,
            Type? exceptionType)
        {
            return string.Join(
                '\t',
                timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                level.ToString(),
                Escape(category),
                eventId.Id.ToString(CultureInfo.InvariantCulture),
                Escape(message),
                Escape(exceptionType?.FullName ?? string.Empty));
        }

        private static string Escape(string value)
        {
            string normalized = value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\r", "\\r", StringComparison.Ordinal)
                .Replace("\n", "\\n", StringComparison.Ordinal)
                .Replace("\t", "\\t", StringComparison.Ordinal);
            return normalized.Length <= MaximumMessageLength
                ? normalized
                : normalized[..MaximumMessageLength];
        }
    }
}
