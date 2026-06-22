// SPDX-License-Identifier: MIT
// Copyright (c) 2025–2026 Vadim Belov <https://belov.us>

namespace Cotton.Mobile.Services
{
    public class CottonCameraBackupSetupDisplayState
    {
        private const string NoDestinationText = "No folder selected";

        private CottonCameraBackupSetupDisplayState(
            string destinationText,
            string executionStatusText,
            string policySummaryText,
            string localMediaRetentionText,
            bool isDestinationSelected,
            bool canEnableBackup)
        {
            DestinationText = destinationText;
            ExecutionStatusText = executionStatusText;
            PolicySummaryText = policySummaryText;
            LocalMediaRetentionText = localMediaRetentionText;
            IsDestinationSelected = isDestinationSelected;
            CanEnableBackup = canEnableBackup;
        }

        public string DestinationText { get; }

        public string ExecutionStatusText { get; }

        public string PolicySummaryText { get; }

        public string LocalMediaRetentionText { get; }

        public bool IsDestinationSelected { get; }

        public bool CanEnableBackup { get; }

        public static CottonCameraBackupSetupDisplayState Create(CottonCameraBackupSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            string destinationText = settings.Destination?.Path ?? NoDestinationText;
            string executionStatusText = settings.HasDestination
                ? "Setup saved. Background backup is not running yet."
                : "Choose a folder before camera backup can run.";
            string policySummaryText = CreatePolicySummary(settings);
            string localMediaRetentionText =
                CottonCameraBackupLocalMediaRetentionPolicy.Mvp.SetupSummaryText;

            return new CottonCameraBackupSetupDisplayState(
                destinationText,
                executionStatusText,
                policySummaryText,
                localMediaRetentionText,
                settings.HasDestination,
                settings.CanRunBackup);
        }

        private static string CreatePolicySummary(CottonCameraBackupSettings settings)
        {
            string media = settings.PhotosOnly ? "Photos only" : "Photos and videos";
            string network = settings.AllowCellular ? "cellular allowed" : "Wi-Fi only";
            string charging = settings.ChargingOnly ? "while charging" : "any battery state";
            return $"{media}, {network}, {charging}.";
        }
    }
}
