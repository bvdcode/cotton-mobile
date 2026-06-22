// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class FeedbackContext
    {
        public FeedbackContext(
            string? instanceUrl,
            string? profileName,
            string screen,
            string fileLocation,
            int visibleFileCount,
            int totalFileCount,
            string fileViewMode,
            string fileSortMode,
            bool isFileSearchActive,
            string? filesStatus,
            bool hasInternetAccess,
            CottonStorageSummary? storageSummary)
        {
            if (string.IsNullOrWhiteSpace(screen))
            {
                throw new ArgumentException("Feedback screen is required.", nameof(screen));
            }

            if (string.IsNullOrWhiteSpace(fileLocation))
            {
                throw new ArgumentException("Feedback file location is required.", nameof(fileLocation));
            }

            if (string.IsNullOrWhiteSpace(fileViewMode))
            {
                throw new ArgumentException("Feedback file view mode is required.", nameof(fileViewMode));
            }

            if (string.IsNullOrWhiteSpace(fileSortMode))
            {
                throw new ArgumentException("Feedback file sort mode is required.", nameof(fileSortMode));
            }

            if (visibleFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(visibleFileCount), "Visible file count cannot be negative.");
            }

            if (totalFileCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(totalFileCount), "Total file count cannot be negative.");
            }

            if (visibleFileCount > totalFileCount)
            {
                throw new ArgumentException("Visible file count cannot exceed total file count.", nameof(visibleFileCount));
            }

            InstanceUrl = CreateOptionalValue(instanceUrl);
            ProfileName = CreateOptionalValue(profileName);
            Screen = screen.Trim();
            FileLocation = fileLocation.Trim();
            VisibleFileCount = visibleFileCount;
            TotalFileCount = totalFileCount;
            FileViewMode = fileViewMode.Trim();
            FileSortMode = fileSortMode.Trim();
            IsFileSearchActive = isFileSearchActive;
            FilesStatus = CreateOptionalValue(filesStatus);
            HasInternetAccess = hasInternetAccess;
            StorageSummary = storageSummary;
        }

        public string? InstanceUrl { get; }

        public string? ProfileName { get; }

        public string Screen { get; }

        public string FileLocation { get; }

        public int VisibleFileCount { get; }

        public int TotalFileCount { get; }

        public string FileViewMode { get; }

        public string FileSortMode { get; }

        public bool IsFileSearchActive { get; }

        public string? FilesStatus { get; }

        public bool HasInternetAccess { get; }

        public CottonStorageSummary? StorageSummary { get; }

        private static string? CreateOptionalValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
