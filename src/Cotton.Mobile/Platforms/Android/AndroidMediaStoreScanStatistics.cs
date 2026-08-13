// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

#if ANDROID
namespace Cotton.Mobile.Platforms.Android
{
    internal class AndroidMediaStoreScanStatistics
    {
        public int HashedFileCount { get; private set; }

        public int ReusedHashCount { get; private set; }

        public void RecordHashedFile()
        {
            HashedFileCount++;
        }

        public void RecordReusedHash()
        {
            ReusedHashCount++;
        }
    }
}
#endif
