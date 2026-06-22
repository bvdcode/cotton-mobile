// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferStagedFileCleanupResult
    {
        public CottonTransferStagedFileCleanupResult(int fileCount, long sizeBytes)
        {
            if (fileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "Cleanup file count cannot be negative.");
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Cleanup size cannot be negative.");
            }

            FileCount = fileCount;
            SizeBytes = sizeBytes;
        }

        public static CottonTransferStagedFileCleanupResult Empty { get; } = new(0, 0);

        public int FileCount { get; }

        public long SizeBytes { get; }

        public bool HasDeletedFiles => FileCount > 0;
    }
}
