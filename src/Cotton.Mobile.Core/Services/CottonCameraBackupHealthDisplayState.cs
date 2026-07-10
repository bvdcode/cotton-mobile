// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupHealthDisplayState
    {
        private CottonCameraBackupHealthDisplayState(
            string title,
            string statusText,
            string countsText,
            bool isBlocked,
            bool hasActivity)
        {
            Title = title;
            StatusText = statusText;
            CountsText = countsText;
            IsBlocked = isBlocked;
            HasActivity = hasActivity;
        }

        public string Title { get; }

        public string StatusText { get; }

        public string CountsText { get; }

        public bool IsBlocked { get; }

        public bool HasActivity { get; }

        public static CottonCameraBackupHealthDisplayState Create(
            CottonCameraBackupSettings settings,
            CottonCameraBackupHealthSnapshot health)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(health);

            string statusText = CreateStatusText(settings, health);

            return new CottonCameraBackupHealthDisplayState(
                "Backup health",
                statusText,
                CreateCountsText(health),
                !settings.CanRunBackup || health.FailedCount > 0 || health.BlockedCount > 0,
                health.HasActivity);
        }

        private static string CreateStatusText(
            CottonCameraBackupSettings settings,
            CottonCameraBackupHealthSnapshot health)
        {
            if (!settings.HasDestination)
            {
                return "Choose a destination to see backup activity.";
            }

            if (!settings.CanRunBackup)
            {
                return "Backup activity is not available yet.";
            }

            if (health.FailedCount > 0 || health.BlockedCount > 0)
            {
                return "Some items need attention.";
            }

            if (health.PendingCount > 0)
            {
                return "Items are waiting to upload.";
            }

            return health.UploadedCount > 0
                ? "Backup activity is up to date."
                : "No backup activity yet.";
        }

        private static string CreateCountsText(CottonCameraBackupHealthSnapshot health)
        {
            List<string> counts = [];
            AddCount(counts, health.PendingCount, "pending");
            AddCount(counts, health.UploadedCount, "uploaded");
            AddCount(counts, health.FailedCount, "failed");
            AddCount(counts, health.BlockedCount, "blocked");
            return string.Join(" · ", counts);
        }

        private static void AddCount(ICollection<string> counts, int count, string label)
        {
            if (count > 0)
            {
                counts.Add($"{count:N0} {label}");
            }
        }
    }
}
