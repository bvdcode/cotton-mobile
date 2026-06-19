namespace Cotton.Mobile.Services
{
    public static class CottonFileKindClassifier
    {
        private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".css",
            ".csv",
            ".htm",
            ".html",
            ".js",
            ".json",
            ".log",
            ".markdown",
            ".md",
            ".svg",
            ".text",
            ".ts",
            ".txt",
            ".xml",
            ".yaml",
            ".yml",
        };

        private static readonly HashSet<string> TextContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/javascript",
            "application/json",
            "application/markdown",
            "application/xml",
            "application/x-yaml",
            "application/yaml",
            "image/svg+xml",
        };

        public static string ResolveKind(string? name, string? contentType)
        {
            string normalizedName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
            string mediaType = CreateContentTypeMediaType(contentType);
            if (IsTextFile(normalizedName, mediaType))
            {
                return "Text";
            }

            if (mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return "Image";
            }

            if (mediaType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(normalizedName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return "PDF";
            }

            if (mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return "Video";
            }

            if (mediaType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
            {
                return "Audio";
            }

            return "File";
        }

        public static string CreateContentTypeMediaType(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return string.Empty;
            }

            string trimmed = contentType.Trim();
            int parameterIndex = trimmed.IndexOf(';', StringComparison.Ordinal);
            string mediaType = parameterIndex < 0
                ? trimmed
                : trimmed[..parameterIndex];
            return mediaType.Trim();
        }

        private static bool IsTextFile(string name, string contentType)
        {
            return contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
                || TextContentTypes.Contains(contentType)
                || TextFileExtensions.Contains(Path.GetExtension(name));
        }
    }
}
