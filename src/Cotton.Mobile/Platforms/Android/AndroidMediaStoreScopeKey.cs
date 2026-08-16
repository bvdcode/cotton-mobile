// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using System.Globalization;

namespace Cotton.Mobile.Platforms.Android
{
    public static class AndroidMediaStoreScopeKey
    {
        private const string Prefix = "buckets:";

        public static string Create(IEnumerable<long> bucketIds)
        {
            AndroidMediaStoreScope scope = new(bucketIds);
            string value = string.Join(
                ",",
                scope.BucketIds
                    .Order()
                    .Select(bucketId => bucketId.ToString(CultureInfo.InvariantCulture)));
            return $"{Prefix}{value}";
        }

        public static bool TryParse(string? scopeKey, out AndroidMediaStoreScope? scope)
        {
            scope = null;
            if (string.IsNullOrWhiteSpace(scopeKey)
                || !scopeKey.StartsWith(Prefix, StringComparison.Ordinal))
            {
                return false;
            }

            List<long> bucketIds = [];
            string value = scopeKey[Prefix.Length..];
            foreach (string candidate in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!long.TryParse(candidate, NumberStyles.Integer, CultureInfo.InvariantCulture, out long bucketId))
                {
                    return false;
                }

                bucketIds.Add(bucketId);
            }

            if (bucketIds.Count == 0)
            {
                return false;
            }

            scope = new AndroidMediaStoreScope(bucketIds);
            return true;
        }
    }
}
#endif
