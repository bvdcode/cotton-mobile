// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonOnDeviceStorageSummary
    {
        private CottonOnDeviceStorageSummary(IReadOnlyList<CottonOnDeviceStorageBucketSnapshot> buckets)
        {
            ArgumentNullException.ThrowIfNull(buckets);

            Buckets = buckets;
            TotalStoredSizeBytes = buckets.Sum(bucket => bucket.SizeBytes);
            TotalItemCount = buckets.Sum(bucket => bucket.ItemCount);
            IsEmpty = TotalStoredSizeBytes == 0 && TotalItemCount == 0;
            SummaryText = IsEmpty
                ? "No offline files or cached previews on this device."
                : $"{CottonFileSizeFormatter.Format(TotalStoredSizeBytes)} stored on this device";
        }

        public IReadOnlyList<CottonOnDeviceStorageBucketSnapshot> Buckets { get; }

        public long TotalStoredSizeBytes { get; }

        public int TotalItemCount { get; }

        public bool IsEmpty { get; }

        public string SummaryText { get; }

        public static CottonOnDeviceStorageSummary Empty { get; } = Create(
            availableOfflineFileCount: 0,
            availableOfflineFileBytes: 0,
            staleOfflineFileCount: 0,
            staleOfflineFileBytes: 0,
            missingOfflineFileCount: 0,
            cachedFolderListingCount: 0,
            cachedFolderListingBytes: 0,
            thumbnailCount: 0,
            thumbnailBytes: 0);

        public static CottonOnDeviceStorageSummary Create(
            int availableOfflineFileCount,
            long availableOfflineFileBytes,
            int staleOfflineFileCount,
            long staleOfflineFileBytes,
            int missingOfflineFileCount,
            int cachedFolderListingCount,
            long cachedFolderListingBytes,
            int thumbnailCount,
            long thumbnailBytes)
        {
            return new CottonOnDeviceStorageSummary(
            [
                CottonOnDeviceStorageBucketSnapshot.CreateAvailableOfflineFiles(
                    availableOfflineFileCount,
                    availableOfflineFileBytes),
                CottonOnDeviceStorageBucketSnapshot.CreateStaleOfflineFiles(
                    staleOfflineFileCount,
                    staleOfflineFileBytes),
                CottonOnDeviceStorageBucketSnapshot.CreateMissingOfflineFiles(missingOfflineFileCount),
                CottonOnDeviceStorageBucketSnapshot.CreateCachedFolderListings(
                    cachedFolderListingCount,
                    cachedFolderListingBytes),
                CottonOnDeviceStorageBucketSnapshot.CreateThumbnails(thumbnailCount, thumbnailBytes),
            ]);
        }
    }
}
