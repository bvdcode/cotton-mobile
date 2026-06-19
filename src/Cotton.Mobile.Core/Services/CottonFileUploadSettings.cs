namespace Cotton.Mobile.Services
{
    public class CottonFileUploadSettings
    {
        public const string SupportedSha256Algorithm = "SHA256";

        public CottonFileUploadSettings(long maxChunkSizeBytes, string? supportedHashAlgorithm)
        {
            if (maxChunkSizeBytes <= 0 || maxChunkSizeBytes > Array.MaxLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxChunkSizeBytes),
                    "Upload chunk size must fit in a single managed buffer.");
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
