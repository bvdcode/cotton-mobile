// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileBulkShareLocalStatusText
    {
        public const string UnavailableStatus = "Selection is no longer available.";

        public const string LocalFilesUnavailableStatus = "Download files before sharing them.";

        public const string CancelledStatus = "Share cancelled.";

        public const string FailedStatus = "Share failed.";

        public static string CreatePreparingStatus(int fileCount)
        {
            int count = NormalizeFileCount(fileCount);
            return count == 1 ? "Preparing file..." : $"Preparing {count} files...";
        }

        public static string CreateSharingStatus(int fileCount)
        {
            int count = NormalizeFileCount(fileCount);
            return count == 1 ? "Sharing file..." : $"Sharing {count} files...";
        }

        private static int NormalizeFileCount(int fileCount)
        {
            if (fileCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileCount), "File count must be positive.");
            }

            return fileCount;
        }
    }
}
