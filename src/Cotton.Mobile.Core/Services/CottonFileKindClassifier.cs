namespace Cotton.Mobile.Services
{
    public static class CottonFileKindClassifier
    {
        private static readonly HashSet<string> TextFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".bash",
            ".c",
            ".cc",
            ".conf",
            ".cpp",
            ".cs",
            ".csproj",
            ".css",
            ".csv",
            ".env",
            ".go",
            ".gradle",
            ".h",
            ".hpp",
            ".htm",
            ".html",
            ".ini",
            ".java",
            ".js",
            ".json",
            ".kt",
            ".kts",
            ".log",
            ".m",
            ".markdown",
            ".md",
            ".mm",
            ".php",
            ".props",
            ".py",
            ".rb",
            ".rs",
            ".sh",
            ".sln",
            ".svg",
            ".swift",
            ".targets",
            ".text",
            ".toml",
            ".ts",
            ".txt",
            ".xml",
            ".yaml",
            ".yml",
            ".zsh",
        };

        private static readonly HashSet<string> TextFileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ".editorconfig",
            ".gitattributes",
            ".gitignore",
            "Dockerfile",
            "LICENSE",
            "Makefile",
            "README",
        };

        private static readonly HashSet<string> TextContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/javascript",
            "application/json",
            "application/markdown",
            "application/typescript",
            "application/xml",
            "application/x-csharp",
            "application/x-python",
            "application/x-sh",
            "application/x-shellscript",
            "application/x-typescript",
            "application/x-yaml",
            "application/yaml",
            "image/svg+xml",
            "text/x-c",
            "text/x-csharp",
            "text/x-java-source",
            "text/x-python",
            "text/x-shellscript",
        };

        private static readonly HashSet<string> DocumentFileExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".doc",
            ".docx",
            ".odp",
            ".ods",
            ".odt",
            ".ppt",
            ".pptx",
            ".rtf",
            ".xls",
            ".xlsx",
        };

        private static readonly HashSet<string> DocumentContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/msword",
            "application/rtf",
            "application/vnd.ms-excel",
            "application/vnd.ms-powerpoint",
            "application/vnd.oasis.opendocument.presentation",
            "application/vnd.oasis.opendocument.spreadsheet",
            "application/vnd.oasis.opendocument.text",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
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

            if (DocumentContentTypes.Contains(mediaType)
                || DocumentFileExtensions.Contains(Path.GetExtension(normalizedName)))
            {
                return "Document";
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
                || TextFileExtensions.Contains(Path.GetExtension(name))
                || TextFileNames.Contains(name);
        }
    }
}
