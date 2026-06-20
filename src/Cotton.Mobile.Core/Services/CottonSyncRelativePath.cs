namespace Cotton.Mobile.Services
{
    public static class CottonSyncRelativePath
    {
        private const char Separator = '/';

        public static string CreateFilePath(string? parentPath, string fileName)
        {
            string normalizedParent = NormalizeFolderPath(parentPath);
            string normalizedFileName = NormalizeSegment(fileName, nameof(fileName));
            return string.IsNullOrEmpty(normalizedParent)
                ? normalizedFileName
                : $"{normalizedParent}{Separator}{normalizedFileName}";
        }

        public static string CreateChildFolderPath(string? parentPath, string folderName)
        {
            string normalizedParent = NormalizeFolderPath(parentPath);
            string normalizedFolderName = NormalizeSegment(folderName, nameof(folderName));
            return string.IsNullOrEmpty(normalizedParent)
                ? normalizedFolderName
                : $"{normalizedParent}{Separator}{normalizedFolderName}";
        }

        public static string NormalizeFilePath(string relativePath, string parameterName)
        {
            string normalizedPath = NormalizePath(relativePath, parameterName);
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                throw new ArgumentException("Sync relative file path is required.", parameterName);
            }

            return normalizedPath;
        }

        public static string NormalizeFolderPath(string? relativePath)
        {
            return string.IsNullOrWhiteSpace(relativePath)
                ? string.Empty
                : NormalizePath(relativePath, nameof(relativePath));
        }

        public static string GetFileName(string relativePath)
        {
            string normalizedPath = NormalizeFilePath(relativePath, nameof(relativePath));
            int separatorIndex = normalizedPath.LastIndexOf(Separator);
            return separatorIndex < 0
                ? normalizedPath
                : normalizedPath[(separatorIndex + 1)..];
        }

        private static string NormalizePath(string relativePath, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                throw new ArgumentException("Sync relative path is required.", parameterName);
            }

            string[] segments = relativePath
                .Split(Separator, StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                throw new ArgumentException("Sync relative path is required.", parameterName);
            }

            return string.Join(Separator, segments.Select(segment => NormalizeSegment(segment, parameterName)));
        }

        private static string NormalizeSegment(string segment, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                throw new ArgumentException("Sync relative path segment is required.", parameterName);
            }

            string normalizedSegment = segment.Trim();
            if (CottonCloudItemNameRules.IsReservedPathSegment(normalizedSegment)
                || CottonCloudItemNameRules.ContainsInvalidCharacter(normalizedSegment))
            {
                throw new ArgumentException("Sync relative path contains an invalid segment.", parameterName);
            }

            return normalizedSegment;
        }
    }
}
