// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonLocalFileSnapshot(string fileName, long sizeBytes, DateTime updatedAtUtc)
    {
        public string FileName { get; } = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;

        public long SizeBytes { get; } = sizeBytes;

        public DateTime UpdatedAtUtc { get; } = updatedAtUtc;
    }
}
