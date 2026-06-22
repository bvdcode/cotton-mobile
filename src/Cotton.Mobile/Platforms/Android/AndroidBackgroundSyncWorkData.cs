// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
using AndroidX.Work;

namespace Cotton.Mobile.Services
{
    internal static class AndroidBackgroundSyncWorkData
    {
        private const string InstanceUriKey = "cotton.instance_uri";
        private const string EligibleRootCountKey = "cotton.eligible_root_count";

        public static Data Create(CottonAndroidBackgroundSyncRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            return new Data.Builder()
                .PutString(InstanceUriKey, request.InstanceUri.AbsoluteUri)
                .PutInt(EligibleRootCountKey, request.EligibleRootCount)
                .Build()
                ?? throw new InvalidOperationException("Android WorkManager sync data builder returned no data.");
        }

        public static bool TryRead(
            Data? data,
            out Uri? instanceUri,
            out int eligibleRootCount)
        {
            instanceUri = null;
            eligibleRootCount = 0;

            string? instanceValue = data?.GetString(InstanceUriKey);
            int rootCountValue = data?.GetInt(EligibleRootCountKey, 0) ?? 0;
            if (!Uri.TryCreate(instanceValue, UriKind.Absolute, out Uri? parsedInstanceUri)
                || rootCountValue <= 0)
            {
                return false;
            }

            instanceUri = parsedInstanceUri;
            eligibleRootCount = rootCountValue;
            return true;
        }
    }
}
#endif
