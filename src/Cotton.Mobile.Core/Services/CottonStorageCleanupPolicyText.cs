namespace Cotton.Mobile.Services
{
    public static class CottonStorageCleanupPolicyText
    {
        public const string CancelAction = "Cancel";
        public const string ClearAllAction = "Clear all";
        public const string ClearDownloadedFilesAction = "Clear downloads";
        public const string ClearFolderListingsAction = "Clear lists";
        public const string ClearThumbnailsAction = "Clear thumbnails";

        public const string ClearAllTitle = "Clear cached and offline files";
        public const string ClearDownloadedFilesTitle = "Clear downloads and kept-offline files";
        public const string ClearFolderListingsTitle = "Clear offline folder lists";
        public const string ClearThumbnailsTitle = "Clear thumbnails";

        public const string ClearThumbnailsMessage =
            "Only cached previews will be removed. Offline files stay on this device.";
        public const string ClearFolderListingsMessage =
            "Saved folder lists will be removed. Offline files stay on this device.";
        public const string ClearDownloadedFilesMessage =
            "Opened downloads and files marked On device, including kept-offline files, will need internet to open again.";
        public const string ClearAllMessage =
            "Cached previews, saved folder lists, opened downloads, and kept-offline files will be removed from this device.";

        public const string ThumbnailsClearedStatus = "Thumbnails cleared.";
        public const string DownloadedFilesClearedStatus = "Downloaded and offline files cleared.";
        public const string FolderListingsClearedStatus = "Offline folder lists cleared.";
        public const string AllCachedFilesClearedStatus = "Cached and offline files cleared.";
    }
}
