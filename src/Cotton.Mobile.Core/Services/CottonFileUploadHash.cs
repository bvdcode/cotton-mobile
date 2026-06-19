using System.Security.Cryptography;

namespace Cotton.Mobile.Services
{
    public static class CottonFileUploadHash
    {
        public static string CreateSha256Hex(ReadOnlySpan<byte> content)
        {
            Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(content, hash);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string FormatHex(byte[] hash)
        {
            ArgumentNullException.ThrowIfNull(hash);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
