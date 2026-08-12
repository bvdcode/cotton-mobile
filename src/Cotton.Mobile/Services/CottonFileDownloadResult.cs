// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileDownloadResult(
        string fileName,
        string filePath,
        long sizeBytes,
        string? contentType = null)
    {
        public string FileName { get; } = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;

        public string FilePath { get; } = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException("File path is required.", nameof(filePath)) : filePath;

        public long SizeBytes { get; } = sizeBytes;

        public string ContentType { get; } = CottonFileOpenRouter.ResolveRequiredContentType(fileName, contentType);
    }
}
