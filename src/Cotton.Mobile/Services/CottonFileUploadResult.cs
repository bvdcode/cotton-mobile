// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileUploadResult
    {
        public CottonFileUploadResult(List<string> chunkHashes, string contentHash, long sizeBytes)
        {
            ArgumentNullException.ThrowIfNull(chunkHashes);
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                throw new ArgumentException("Content hash is required.", nameof(contentHash));
            }

            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Uploaded size cannot be negative.");
            }

            ChunkHashes = chunkHashes;
            ContentHash = contentHash;
            SizeBytes = sizeBytes;
        }

        public List<string> ChunkHashes { get; }

        public string ContentHash { get; }

        public long SizeBytes { get; }
    }
}
