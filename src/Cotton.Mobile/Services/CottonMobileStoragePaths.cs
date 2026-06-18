using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public static class CottonMobileStoragePaths
    {
        private const int MaxSafeFileNameLength = 120;
        private const int TruncatedFileNameHashLength = 12;
        private const int MaxSafeFileExtensionLength = 24;
        private const string DefaultDownloadFileName = "download";
        private const string TemporaryDownloadDirectoryName = ".temp";
        private const string TemporaryDownloadFileExtension = ".download";

        public const string DownloadDirectoryName = "CottonDownloads";

        public static string CreateDownloadsDirectory()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DownloadDirectoryName);
        }

        public static string CreateTemporaryDownloadsDirectory()
        {
            return Path.Combine(CreateDownloadsDirectory(), TemporaryDownloadDirectoryName);
        }

        public static string CreateDownloadDirectory(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Path.Combine(
                CreateDownloadsDirectory(),
                CreateInstanceStorageKey(instanceUri),
                file.Id.ToString("D"));
        }

        public static string CreateDownloadPath(Uri instanceUri, CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(instanceUri);
            ArgumentNullException.ThrowIfNull(file);

            return Path.Combine(CreateDownloadDirectory(instanceUri, file), CreateSafeFileName(file.Name));
        }

        public static string CreateTemporaryDownloadPath()
        {
            return Path.Combine(CreateTemporaryDownloadsDirectory(), $"{Guid.NewGuid():N}{TemporaryDownloadFileExtension}");
        }

        public static string CreateThumbnailCacheDirectory(FileThumbnailCacheOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return Path.Combine(FileSystem.AppDataDirectory, options.DirectoryName);
        }

        public static bool IsTemporaryDownloadPath(string path)
        {
            string temporaryDownloadsDirectory = Path.GetFullPath(CreateTemporaryDownloadsDirectory());
            string fullPath = Path.GetFullPath(path);
            string relativePath = Path.GetRelativePath(temporaryDownloadsDirectory, fullPath);
            return !Path.IsPathRooted(relativePath)
                && !string.Equals(relativePath, ".", StringComparison.Ordinal)
                && !string.Equals(relativePath, "..", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !relativePath.StartsWith($"..{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal);
        }

        public static string CreateInstanceStorageKey(Uri instanceUri)
        {
            CottonInstanceUri.EnsureSupported(instanceUri, nameof(instanceUri));

            string scheme = instanceUri.Scheme.ToLowerInvariant();
            string host = instanceUri.Host.ToLowerInvariant();
            string authority = instanceUri.IsDefaultPort ? host : $"{host}:{instanceUri.Port}";
            string path = NormalizePath(instanceUri.AbsolutePath);
            string scope = $"{scheme}://{authority}{path}";
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope))).ToLowerInvariant();
        }

        private static string CreateSafeFileName(string fileName)
        {
            string trimmedName = string.IsNullOrWhiteSpace(fileName) ? DefaultDownloadFileName : fileName.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var buffer = new char[trimmedName.Length];
            for (int index = 0; index < trimmedName.Length; index++)
            {
                char character = trimmedName[index];
                buffer[index] = invalidChars.Contains(character) ? '_' : character;
            }

            string safeName = new string(buffer).Trim();
            if (string.IsNullOrWhiteSpace(safeName) || IsReservedPathSegment(safeName))
            {
                return DefaultDownloadFileName;
            }

            return TruncateSafeFileName(safeName);
        }

        private static string TruncateSafeFileName(string safeName)
        {
            if (safeName.Length <= MaxSafeFileNameLength)
            {
                return safeName;
            }

            string extension = Path.GetExtension(safeName);
            if (extension.Length > MaxSafeFileExtensionLength)
            {
                extension = string.Empty;
            }

            string nameWithoutExtension = Path.GetFileNameWithoutExtension(safeName);
            if (string.IsNullOrWhiteSpace(nameWithoutExtension) || IsReservedPathSegment(nameWithoutExtension))
            {
                nameWithoutExtension = DefaultDownloadFileName;
            }

            string hash = CreateShortHash(safeName);
            int maxNameLength = MaxSafeFileNameLength - extension.Length - hash.Length - 1;
            if (maxNameLength < DefaultDownloadFileName.Length)
            {
                maxNameLength = DefaultDownloadFileName.Length;
                extension = string.Empty;
            }

            string truncatedName = nameWithoutExtension.Length <= maxNameLength
                ? nameWithoutExtension
                : nameWithoutExtension[..maxNameLength].Trim();
            if (string.IsNullOrWhiteSpace(truncatedName) || IsReservedPathSegment(truncatedName))
            {
                truncatedName = DefaultDownloadFileName;
            }

            return $"{truncatedName}-{hash}{extension}";
        }

        private static string CreateShortHash(string value)
        {
            return Convert
                .ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
                .ToLowerInvariant()[..TruncatedFileNameHashLength];
        }

        private static bool IsReservedPathSegment(string value)
        {
            return string.Equals(value, ".", StringComparison.Ordinal)
                || string.Equals(value, "..", StringComparison.Ordinal);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || string.Equals(path, "/", StringComparison.Ordinal))
            {
                return string.Empty;
            }

            return path.TrimEnd('/');
        }
    }
}
