// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public static class CottonFileTrashStatusText
    {
        public const string CancelledStatus = "Move to trash cancelled.";

        public const string ConfirmAction = "Move to trash";

        public const string ConfirmTitle = "Move to trash?";

        public const string FailedStatus = "Could not move file to trash.";

        public const string NeedsRefreshStatus = "Refresh this folder before moving this file to trash.";

        public const string OfflineUnavailableStatus = "Offline. Move to trash needs internet.";

        public const string TimedOutStatus = "Move to trash is taking longer than expected. Refresh and try again.";

        public static string CreateConfirmMessage(string fileName)
        {
            string name = NormalizeFileName(fileName);
            return $"{name} will be removed from this folder and can be restored from trash.";
        }

        public static string CreateMovedStatus(string fileName)
        {
            string name = NormalizeFileName(fileName);
            return $"{name} moved to trash.";
        }

        public static string CreateMovingStatus(string fileName)
        {
            string name = NormalizeFileName(fileName);
            return $"Moving {name} to trash...";
        }

        private static string NormalizeFileName(string fileName)
        {
            return string.IsNullOrWhiteSpace(fileName) ? "File" : fileName.Trim();
        }
    }
}
