namespace Cotton.Mobile.Services
{
    public class CottonFileUploadSourceSnapshot
    {
        public const string DefaultContentType = "application/octet-stream";
        public const string DefaultFileName = "Uploaded file";

        public CottonFileUploadSourceSnapshot(string? name, string? contentType, long? sizeBytes)
        {
            if (sizeBytes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Upload size cannot be negative.");
            }

            Name = NormalizeName(name);
            ContentType = NormalizeContentType(contentType);
            SizeBytes = sizeBytes;
        }

        public string Name { get; }

        public string ContentType { get; }

        public long? SizeBytes { get; }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return DefaultFileName;
            }

            string pathSafeName = name.Trim()
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar);
            string fileName = Path.GetFileName(pathSafeName);
            return string.IsNullOrWhiteSpace(fileName) ? DefaultFileName : fileName.Trim();
        }

        private static string NormalizeContentType(string? contentType)
        {
            return string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType.Trim();
        }
    }
}
