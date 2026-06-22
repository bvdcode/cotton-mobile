// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonLocalFileSnapshot
    {
        public CottonLocalFileSnapshot(string fileName, long sizeBytes, DateTime updatedAtUtc)
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;
            SizeBytes = sizeBytes;
            UpdatedAtUtc = updatedAtUtc;
        }

        public string FileName { get; }

        public long SizeBytes { get; }

        public DateTime UpdatedAtUtc { get; }
    }
}
