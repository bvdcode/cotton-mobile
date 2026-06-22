// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileVersionStatusText
    {
        public const string CancelledStatus = "Version history cancelled.";

        public const string FailedStatus = "Could not load versions.";

        public const string OfflineUnavailableStatus = "Offline. Version history needs internet.";

        public static string CreateLoadingStatus(string fileName)
        {
            string name = string.IsNullOrWhiteSpace(fileName) ? "file" : fileName.Trim();
            return $"Loading versions for {name}...";
        }

        public static string CreateLoadedStatus(int versionCount)
        {
            if (versionCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(versionCount), "Version count cannot be negative.");
            }

            return versionCount == 1
                ? "1 version found."
                : $"{versionCount:N0} versions found.";
        }
    }
}
