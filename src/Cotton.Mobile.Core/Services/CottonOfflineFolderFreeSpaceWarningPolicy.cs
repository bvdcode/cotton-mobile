// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonOfflineFolderFreeSpaceWarningPolicy
    {
        public const long LargeFolderWarningBytes = 100L * 1024L * 1024L;
        public const long MinimumFreeSpaceAfterDownloadBytes = 512L * 1024L * 1024L;

        public static CottonOfflineFolderFreeSpaceWarning CreateWarning(
            CottonOfflineDownloadQueueSnapshot queue,
            CottonDeviceStorageSpaceSnapshot storageSpace)
        {
            ArgumentNullException.ThrowIfNull(queue);
            ArgumentNullException.ThrowIfNull(storageSpace);

            if (!storageSpace.HasAvailableSpace)
            {
                return queue.TotalSizeBytes >= LargeFolderWarningBytes
                    ? CreateUnknownFreeSpaceWarning(queue)
                    : CottonOfflineFolderFreeSpaceWarning.None;
            }

            long availableBytes = storageSpace.AvailableBytes!.Value;
            if (queue.TotalSizeBytes > availableBytes)
            {
                return CreateNotEnoughFreeSpaceWarning(queue, availableBytes);
            }

            long remainingBytes = availableBytes - queue.TotalSizeBytes;
            if (remainingBytes < MinimumFreeSpaceAfterDownloadBytes)
            {
                return CreateLowFreeSpaceWarning(queue, availableBytes, remainingBytes);
            }

            return queue.TotalSizeBytes >= LargeFolderWarningBytes
                ? CreateLargeFolderWarning(queue, availableBytes)
                : CottonOfflineFolderFreeSpaceWarning.None;
        }

        private static CottonOfflineFolderFreeSpaceWarning CreateLargeFolderWarning(
            CottonOfflineDownloadQueueSnapshot queue,
            long availableBytes)
        {
            return new CottonOfflineFolderFreeSpaceWarning(
                CottonOfflineFolderFreeSpaceWarningKind.LargeFolder,
                "Keep large folder offline?",
                $"{queue.FolderName} will use {FormatSize(queue.TotalSizeBytes)} on this device. {FormatSize(availableBytes)} is free.");
        }

        private static CottonOfflineFolderFreeSpaceWarning CreateLowFreeSpaceWarning(
            CottonOfflineDownloadQueueSnapshot queue,
            long availableBytes,
            long remainingBytes)
        {
            return new CottonOfflineFolderFreeSpaceWarning(
                CottonOfflineFolderFreeSpaceWarningKind.LowFreeSpaceAfterDownload,
                "Free device space will be low",
                $"{queue.FolderName} will use {FormatSize(queue.TotalSizeBytes)} and may leave only {FormatSize(remainingBytes)} free. {FormatSize(availableBytes)} is free now.");
        }

        private static CottonOfflineFolderFreeSpaceWarning CreateNotEnoughFreeSpaceWarning(
            CottonOfflineDownloadQueueSnapshot queue,
            long availableBytes)
        {
            return new CottonOfflineFolderFreeSpaceWarning(
                CottonOfflineFolderFreeSpaceWarningKind.NotEnoughFreeSpace,
                "Device storage may be full",
                $"{queue.FolderName} needs {FormatSize(queue.TotalSizeBytes)}, but this device reports {FormatSize(availableBytes)} free. The download may fail.");
        }

        private static CottonOfflineFolderFreeSpaceWarning CreateUnknownFreeSpaceWarning(
            CottonOfflineDownloadQueueSnapshot queue)
        {
            return new CottonOfflineFolderFreeSpaceWarning(
                CottonOfflineFolderFreeSpaceWarningKind.UnknownFreeSpaceForLargeFolder,
                "Keep large folder offline?",
                $"{queue.FolderName} will use {FormatSize(queue.TotalSizeBytes)}. Free device space could not be checked.");
        }

        private static string FormatSize(long sizeBytes)
        {
            return CottonFileSizeFormatter.Format(sizeBytes);
        }
    }
}
