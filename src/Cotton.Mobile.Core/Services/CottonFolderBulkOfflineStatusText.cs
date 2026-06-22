// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFolderBulkOfflineStatusText
    {
        private const string DefaultFolderName = "Folder";

        public static string UnavailableStatus { get; } = "Selection is no longer available.";

        public static string CreateStartingStatus(int folderCount)
        {
            ValidateCount(folderCount, nameof(folderCount));

            return folderCount == 1
                ? "Checking folder for offline use..."
                : $"Checking {folderCount} folders for offline use...";
        }

        public static string CreateCheckingFolderStatus(
            int position,
            int folderCount,
            string folderName)
        {
            ValidateProgress(position, folderCount);

            return $"Checking {position} of {folderCount} folders: {NormalizeFolderName(folderName)}...";
        }

        private static void ValidateProgress(int position, int folderCount)
        {
            ValidateCount(position, nameof(position));
            ValidateCount(folderCount, nameof(folderCount));
            if (position > folderCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    "Position must be within the selected folder count.");
            }
        }

        private static void ValidateCount(int count, string parameterName)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Count must be positive.");
            }
        }

        private static string NormalizeFolderName(string folderName)
        {
            return string.IsNullOrWhiteSpace(folderName) ? DefaultFolderName : folderName.Trim();
        }
    }
}
