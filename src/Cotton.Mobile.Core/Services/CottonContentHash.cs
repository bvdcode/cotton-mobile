// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

using System.Security.Cryptography;

namespace Cotton.Mobile.Services
{
    public static class CottonContentHash
    {
        public const int Sha256HexLength = SHA256.HashSizeInBytes * 2;

        public static string ComputeSha256(Stream content, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            byte[] buffer = new byte[81920];
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int bytesRead = content.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, bytesRead);
            }

            return FormatSha256(hash.GetHashAndReset());
        }

        public static async Task<string> ComputeSha256Async(
            Stream content,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(content);

            byte[] buffer = new byte[81920];
            using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            while (true)
            {
                int bytesRead = await content.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    break;
                }

                hash.AppendData(buffer, 0, bytesRead);
            }

            return FormatSha256(hash.GetHashAndReset());
        }

        public static string ComputeSha256(ReadOnlySpan<byte> content)
        {
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(content, hash);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string FormatSha256(byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            if (hash.Length != SHA256.HashSizeInBytes)
            {
                throw new ArgumentException("SHA-256 hash must contain 32 bytes.", nameof(hash));
            }

            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string NormalizeSha256(string hash, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                throw new ArgumentException("SHA-256 content hash is required.", parameterName);
            }

            string normalized = hash.Trim().ToLowerInvariant();
            if (normalized.Length != Sha256HexLength || normalized.Any(character => !Uri.IsHexDigit(character)))
            {
                throw new ArgumentException("Content hash must be a 64-character SHA-256 hexadecimal value.", parameterName);
            }

            return normalized;
        }

        public static string? NormalizeOptionalSha256(string? hash, string parameterName)
        {
            return string.IsNullOrWhiteSpace(hash) ? null : NormalizeSha256(hash, parameterName);
        }
    }
}
