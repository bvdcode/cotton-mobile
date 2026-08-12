// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonFileUploadSettings
    {
        public const int MinimumChunkSizeBytes = 4 * 1024 * 1024;
        public const int MaximumChunkSizeBytes = 16 * 1024 * 1024;
        public const string SupportedSha256Algorithm = "SHA256";

        public CottonFileUploadSettings(long maxChunkSizeBytes, string? supportedHashAlgorithm)
        {
            if (maxChunkSizeBytes < MinimumChunkSizeBytes || maxChunkSizeBytes > MaximumChunkSizeBytes)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxChunkSizeBytes),
                    $"Upload chunk size must be between {MinimumChunkSizeBytes} and {MaximumChunkSizeBytes} bytes.");
            }

            if (!string.Equals(
                    supportedHashAlgorithm?.Trim(),
                    SupportedSha256Algorithm,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException("Cotton Mobile currently supports SHA256 uploads only.");
            }

            MaxChunkSizeBytes = (int)maxChunkSizeBytes;
            SupportedHashAlgorithm = SupportedSha256Algorithm;
        }

        public int MaxChunkSizeBytes { get; }

        public string SupportedHashAlgorithm { get; }
    }
}
