// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Globalization;

namespace Cotton.Mobile.Services
{
    public static class CottonDiagnosticJournalTransfer
    {
        private const string HeaderPrefix = "BEGIN records=";
        private const string Footer = "END";

        public static async Task WriteAsync(TextWriter writer, IReadOnlyList<string> records)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(records);
            await writer.WriteLineAsync(HeaderPrefix + records.Count.ToString(CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
            foreach (string record in records)
            {
                await writer.WriteLineAsync(record).ConfigureAwait(false);
            }

            await writer.WriteLineAsync(Footer).ConfigureAwait(false);
        }

        public static void Validate(string export)
        {
            ArgumentNullException.ThrowIfNull(export);
            using StringReader reader = new(export);
            string? header = reader.ReadLine();
            if (header is null || !header.StartsWith(HeaderPrefix, StringComparison.Ordinal)
                || !int.TryParse(header.AsSpan(HeaderPrefix.Length), NumberStyles.None,
                    CultureInfo.InvariantCulture, out int recordCount))
            {
                throw new InvalidDataException("Diagnostic journal header is invalid.");
            }

            for (int index = 0; index < recordCount; index++)
            {
                string? record = reader.ReadLine();
                if (record is null || string.Equals(record, Footer, StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Diagnostic journal records are incomplete.");
                }
            }

            if (!string.Equals(reader.ReadLine(), Footer, StringComparison.Ordinal)
                || reader.ReadLine() is not null)
            {
                throw new InvalidDataException("Diagnostic journal footer is invalid.");
            }
        }
    }
}
