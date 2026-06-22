// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTextPreviewDisplayState
    {
        private CottonTextPreviewDisplayState(
            string kind,
            long sizeBytes,
            int lineCount)
        {
            Kind = string.IsNullOrWhiteSpace(kind) ? "Text" : kind.Trim();
            SizeBytes = sizeBytes < 0 ? 0 : sizeBytes;
            LineCount = lineCount < 0 ? 0 : lineCount;
            DetailsText = $"{Kind} · {FormatLineCount(LineCount)}";
        }

        public string Kind { get; }

        public long SizeBytes { get; }

        public int LineCount { get; }

        public string DetailsText { get; }

        public static CottonTextPreviewDisplayState Create(
            string? kind,
            long sizeBytes,
            string content)
        {
            ArgumentNullException.ThrowIfNull(content);

            return new CottonTextPreviewDisplayState(
                string.IsNullOrWhiteSpace(kind) ? "Text" : kind,
                sizeBytes,
                CountLines(content));
        }

        public static int CountLines(string content)
        {
            ArgumentNullException.ThrowIfNull(content);
            if (content.Length == 0)
            {
                return 0;
            }

            int count = 1;
            for (int index = 0; index < content.Length; index++)
            {
                char value = content[index];
                if (value == '\n')
                {
                    count++;
                    continue;
                }

                if (value == '\r'
                    && (index + 1 >= content.Length || content[index + 1] != '\n'))
                {
                    count++;
                }
            }

            return count;
        }

        private static string FormatLineCount(int lineCount)
        {
            return lineCount == 1
                ? "1 line"
                : $"{lineCount} lines";
        }
    }
}
