using Microsoft.Maui.Storage;
using System.Security.Cryptography;
using System.Text;

namespace Cotton.Mobile.Services
{
    public static class CottonMobileStoragePaths
    {
        public const string DownloadDirectoryName = "CottonDownloads";

        public static string CreateDownloadsDirectory()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DownloadDirectoryName);
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

        public static string CreateThumbnailCacheDirectory(FileThumbnailCacheOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return Path.Combine(FileSystem.AppDataDirectory, options.DirectoryName);
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
            string trimmedName = string.IsNullOrWhiteSpace(fileName) ? "download" : fileName.Trim();
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var buffer = new char[trimmedName.Length];
            for (int index = 0; index < trimmedName.Length; index++)
            {
                char character = trimmedName[index];
                buffer[index] = invalidChars.Contains(character) ? '_' : character;
            }

            return new string(buffer);
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
