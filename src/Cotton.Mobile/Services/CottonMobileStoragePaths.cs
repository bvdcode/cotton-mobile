using Microsoft.Maui.Storage;

namespace Cotton.Mobile.Services
{
    public static class CottonMobileStoragePaths
    {
        public const string DownloadDirectoryName = "CottonDownloads";

        public static string CreateDownloadsDirectory()
        {
            return Path.Combine(FileSystem.AppDataDirectory, DownloadDirectoryName);
        }

        public static string CreateDownloadDirectory(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return Path.Combine(CreateDownloadsDirectory(), file.Id.ToString("D"));
        }

        public static string CreateDownloadPath(CottonFileBrowserEntry file)
        {
            ArgumentNullException.ThrowIfNull(file);

            return Path.Combine(CreateDownloadDirectory(file), CreateSafeFileName(file.Name));
        }

        public static string CreateThumbnailCacheDirectory(FileThumbnailCacheOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            return Path.Combine(FileSystem.AppDataDirectory, options.DirectoryName);
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
    }
}
