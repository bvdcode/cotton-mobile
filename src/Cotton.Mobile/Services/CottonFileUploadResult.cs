// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileUploadResult
    {
        public CottonFileUploadResult(List<string> chunkHashes, string contentHash)
        {
            ArgumentNullException.ThrowIfNull(chunkHashes);
            if (string.IsNullOrWhiteSpace(contentHash))
            {
                throw new ArgumentException("Content hash is required.", nameof(contentHash));
            }

            ChunkHashes = chunkHashes;
            ContentHash = contentHash;
        }

        public List<string> ChunkHashes { get; }

        public string ContentHash { get; }
    }
}
