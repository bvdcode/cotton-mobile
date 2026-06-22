// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonTransferStagedFileSnapshot
    {
        public CottonTransferStagedFileSnapshot(Guid transferId, string fileName, string path, long sizeBytes)
        {
            if (transferId == Guid.Empty)
            {
                throw new ArgumentException("Transfer id cannot be empty.", nameof(transferId));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Staged file name is required.", nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Staged file path is required.", nameof(path));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Staged file size cannot be negative.");
            }

            TransferId = transferId;
            FileName = fileName.Trim();
            Path = path;
            SizeBytes = sizeBytes;
        }

        public Guid TransferId { get; }

        public string FileName { get; }

        public string Path { get; }

        public long SizeBytes { get; }
    }
}
