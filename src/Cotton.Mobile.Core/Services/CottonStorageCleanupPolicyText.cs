// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonStorageCleanupPolicyText
    {
        public const string CancelAction = "Cancel";
        public const string ClearAllAction = "Clear all";
        public const string ClearDownloadedFilesAction = "Clear downloads";
        public const string ClearFolderListingsAction = "Clear lists";
        public const string ClearThumbnailsAction = "Clear thumbnails";
        public const string ClearTemporaryUploadsAction = "Clear temp uploads";
        public const string FreeDeviceSpaceAction = "Free space";

        public const string ClearAllTitle = "Clear cached and offline files";
        public const string ClearDownloadedFilesTitle = "Clear downloads and kept-offline files";
        public const string ClearFolderListingsTitle = "Clear offline folder lists";
        public const string ClearThumbnailsTitle = "Clear thumbnails";
        public const string ClearTemporaryUploadsTitle = "Clear temporary upload files";
        public const string FreeDeviceSpaceTitle = "Free device space";

        public const string ClearThumbnailsMessage =
            "Only cached previews will be removed. Offline files stay on this device.";
        public const string ClearFolderListingsMessage =
            "Saved folder lists will be removed. Offline files stay on this device.";
        public const string ClearDownloadedFilesMessage =
            "Opened downloads and files marked On device, including kept-offline files, will need internet to open again.";
        public const string ClearTemporaryUploadsMessage =
            "Only completed, cancelled, or abandoned upload files for this account will be removed. Waiting, running, and failed uploads stay in Transfers.";
        public const string ClearAllMessage =
            "Cached previews, saved folder lists, opened downloads, and kept-offline files will be removed from this device.";
        public const string FreeDeviceSpaceMessage =
            "Opened downloads not kept offline, cached previews, and saved folder lists will be removed. Kept-offline files and waiting, running, or failed uploads stay on this device.";

        public const string ThumbnailsClearedStatus = "Thumbnails cleared.";
        public const string DownloadedFilesClearedStatus = "Downloaded and offline files cleared.";
        public const string FolderListingsClearedStatus = "Offline folder lists cleared.";
        public const string AllCachedFilesClearedStatus = "Cached and offline files cleared.";

        public static string CreateTemporaryUploadsClearedStatus(CottonTransferStagedFileCleanupResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (!result.HasDeletedFiles)
            {
                return "No temporary upload files to clear.";
            }

            string sizeText = CottonFileSizeFormatter.Format(result.SizeBytes);
            return result.FileCount == 1
                ? $"1 temporary upload file cleared ({sizeText})."
                : $"{result.FileCount:N0} temporary upload files cleared ({sizeText}).";
        }

        public static string CreateDeviceSpaceFreedStatus(CottonDeviceSpaceCleanupResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            if (!result.HasDeletedFiles)
            {
                return "No evictable Cotton files to clear.";
            }

            string sizeText = CottonFileSizeFormatter.Format(result.SizeBytes);
            return result.FileCount == 1
                ? $"Freed {sizeText} from 1 Cotton file."
                : $"Freed {sizeText} from {result.FileCount:N0} Cotton files.";
        }
    }
}
