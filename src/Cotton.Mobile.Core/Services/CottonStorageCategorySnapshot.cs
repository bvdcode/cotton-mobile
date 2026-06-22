// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonStorageCategorySnapshot
    {
        public CottonStorageCategorySnapshot(string name, long sizeBytes, int fileCount)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Storage category name is required.", nameof(name));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Storage category size cannot be negative.");
            }

            if (fileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "Storage category file count cannot be negative.");
            }

            Name = name.Trim();
            SizeBytes = sizeBytes;
            FileCount = fileCount;
        }

        public string Name { get; }

        public long SizeBytes { get; }

        public int FileCount { get; }
    }
}
