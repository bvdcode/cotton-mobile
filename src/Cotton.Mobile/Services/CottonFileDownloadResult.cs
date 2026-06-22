// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileDownloadResult
    {
        public CottonFileDownloadResult(
            string fileName,
            string filePath,
            long sizeBytes,
            string? contentType = null)
        {
            FileName = string.IsNullOrWhiteSpace(fileName) ? throw new ArgumentException("File name is required.", nameof(fileName)) : fileName;
            FilePath = string.IsNullOrWhiteSpace(filePath) ? throw new ArgumentException("File path is required.", nameof(filePath)) : filePath;
            SizeBytes = sizeBytes;
            ContentType = CottonFileOpenRouter.ResolveRequiredContentType(fileName, contentType);
        }

        public string FileName { get; }

        public string FilePath { get; }

        public long SizeBytes { get; }

        public string ContentType { get; }
    }
}
