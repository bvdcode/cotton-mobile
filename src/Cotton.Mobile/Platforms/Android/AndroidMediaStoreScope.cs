// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    public class AndroidMediaStoreScope
    {
        public AndroidMediaStoreScope(IEnumerable<long> bucketIds)
        {
            ArgumentNullException.ThrowIfNull(bucketIds);

            HashSet<long> uniqueBucketIds = [.. bucketIds];
            if (uniqueBucketIds.Count == 0)
            {
                throw new ArgumentException("At least one Android media bucket is required.", nameof(bucketIds));
            }

            BucketIds = uniqueBucketIds;
        }

        public IReadOnlySet<long> BucketIds { get; }
    }
}
#endif
